<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { createNote, deleteNote, fetchNotes, updateNote } from "../api/notes";
import type { Note, SortOption, User } from "../types";

const props = defineProps<{
  currentUser: User;
}>();

const emit = defineEmits<{
  (e: "logout"): void;
}>();

const notes = ref<Note[]>([]);
const isLoading = ref(true);
const loadError = ref("");

const searchQuery = ref("");
const sortOption = ref<SortOption>("newest");

const isModalOpen = ref(false);
const modalMode = ref<"create" | "edit">("create");
const editingNoteId = ref<string | null>(null);
const editingNoteMeta = ref<{ createdAt: string; updatedAt: string } | null>(null);
const isSaving = ref(false);
const modalError = ref("");

const noteForm = reactive({
  title: "",
  content: "",
  isPinned: false,
});

const deletingId = ref<string | null>(null);

async function loadNotes() {
  isLoading.value = true;
  loadError.value = "";
  try {
    notes.value = await fetchNotes();
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : "Failed to load notes.";
  } finally {
    isLoading.value = false;
  }
}

onMounted(loadNotes);

const filteredSortedNotes = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();

  let result = notes.value.filter((note) => {
    if (!query) return true;
    return (
      note.title.toLowerCase().includes(query) ||
      note.content.toLowerCase().includes(query)
    );
  });

  result = [...result].sort((a, b) => {
    switch (sortOption.value) {
      case "oldest":
        return new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      case "alphabetical":
        return a.title.localeCompare(b.title);
      case "newest":
      default:
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    }
  });

  // Pinned notes always float to the top, regardless of sort choice.
  return [...result].sort((a, b) => Number(b.isPinned) - Number(a.isPinned));
});

const initials = computed(() => {
  const parts = props.currentUser.displayName.trim().split(/\s+/);
  return parts
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? "")
    .join("");
});

function openCreateModal() {
  modalMode.value = "create";
  editingNoteId.value = null;
  editingNoteMeta.value = null;
  noteForm.title = "";
  noteForm.content = "";
  noteForm.isPinned = false;
  modalError.value = "";
  isModalOpen.value = true;
}

function openEditModal(note: Note) {
  modalMode.value = "edit";
  editingNoteId.value = note.id;
  editingNoteMeta.value = { createdAt: note.createdAt, updatedAt: note.updatedAt };
  noteForm.title = note.title;
  noteForm.content = note.content;
  noteForm.isPinned = note.isPinned;
  modalError.value = "";
  isModalOpen.value = true;
}

function closeModal() {
  isModalOpen.value = false;
}

async function submitNote() {
  if (!noteForm.title.trim()) {
    modalError.value = "Title is required.";
    return;
  }

  isSaving.value = true;
  modalError.value = "";

  try {
    if (modalMode.value === "create") {
      const created = await createNote({
        title: noteForm.title.trim(),
        content: noteForm.content,
        isPinned: noteForm.isPinned,
      });
      notes.value = [created, ...notes.value];
    } else if (editingNoteId.value) {
      const updated = await updateNote(editingNoteId.value, {
        title: noteForm.title.trim(),
        content: noteForm.content,
        isPinned: noteForm.isPinned,
      });
      notes.value = notes.value.map((n) => (n.id === updated.id ? updated : n));
    }
    isModalOpen.value = false;
  } catch (err) {
    modalError.value = err instanceof Error ? err.message : "Failed to save note.";
  } finally {
    isSaving.value = false;
  }
}

async function handleDelete(note: Note) {
  deletingId.value = note.id;
  try {
    await deleteNote(note.id);
    notes.value = notes.value.filter((n) => n.id !== note.id);
  } catch (err) {
    loadError.value = err instanceof Error ? err.message : "Failed to delete note.";
  } finally {
    deletingId.value = null;
  }
}

async function handleDeleteFromModal() {
  if (!editingNoteId.value) return;

  const id = editingNoteId.value;
  deletingId.value = id;
  modalError.value = "";

  try {
    await deleteNote(id);
    notes.value = notes.value.filter((n) => n.id !== id);
    isModalOpen.value = false;
  } catch (err) {
    modalError.value = err instanceof Error ? err.message : "Failed to delete note.";
  } finally {
    deletingId.value = null;
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric",
  });
}
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <header class="border-b border-gray-200 bg-white sticky top-0 z-10">
      <div class="max-w-6xl mx-auto px-4 sm:px-6 py-4 flex items-center justify-between gap-4">
        <div class="flex items-center gap-2.5">
          <h1 class="text-gray-900 font-semibold text-lg tracking-tight">Techbodia Notes</h1>
        </div>

        <div class="flex items-center gap-3">
          <div class="hidden sm:flex items-center gap-2 text-sm text-gray-500">
            <div class="w-7 h-7 rounded-full bg-indigo-600 text-white text-xs font-semibold flex items-center justify-center">
              {{ initials }}
            </div>
            <span>{{ currentUser.displayName }}</span>
          </div>
          <button
            class="text-sm text-gray-500 hover:text-gray-900 border border-gray-300 hover:border-gray-400 rounded-lg px-3 py-1.5 transition"
            @click="emit('logout')"
          >
            Log out
          </button>
        </div>
      </div>
    </header>

    <main class="max-w-6xl mx-auto px-4 sm:px-6 py-6">
      <div class="flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between mb-6">
        <div class="flex flex-1 gap-3 flex-col sm:flex-row">
          <div class="relative flex-1 max-w-md">
            <svg xmlns="http://www.w3.org/2000/svg" class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
            </svg>
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Search notes..."
              class="w-full rounded-lg bg-white border border-gray-300 text-gray-900 placeholder-gray-400 pl-9 pr-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
            />
          </div>

          <div class="relative">
            <select
              v-model="sortOption"
              class="appearance-none rounded-lg bg-white border border-gray-300 text-gray-700 pl-3.5 pr-9 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 transition"
            >
              <option value="newest">Newest first</option>
              <option value="oldest">Oldest first</option>
              <option value="alphabetical">Alphabetical</option>
            </select>
            <svg xmlns="http://www.w3.org/2000/svg" class="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5" />
            </svg>
          </div>
        </div>

        <button
          class="inline-flex items-center justify-center gap-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white font-medium text-sm px-4 py-2.5 transition shadow-sm whitespace-nowrap"
          @click="openCreateModal"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
          </svg>
          New note
        </button>
      </div>

      <p v-if="loadError" class="text-sm text-rose-600 bg-rose-50 border border-rose-200 rounded-lg px-3.5 py-2.5 mb-5">
        {{ loadError }}
      </p>

      <div v-if="isLoading" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div v-for="n in 6" :key="n" class="h-36 rounded-xl bg-white border border-gray-200 animate-pulse"></div>
      </div>

      <div
        v-else-if="filteredSortedNotes.length === 0"
        class="flex flex-col items-center justify-center text-center py-20 text-gray-400"
      >
        <svg xmlns="http://www.w3.org/2000/svg" class="h-10 w-10 mb-3 opacity-60" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" />
        </svg>
        <p class="text-sm">{{ searchQuery ? "No notes match your search." : "No notes yet — create your first one." }}</p>
      </div>

      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <article
          v-for="note in filteredSortedNotes"
          :key="note.id"
          class="group flex flex-col rounded-xl bg-white border border-gray-200 hover:border-gray-300 hover:shadow-sm p-4 transition cursor-pointer"
          @click="openEditModal(note)"
        >
          <div class="flex items-start justify-between gap-2 mb-2">
            <h3 class="text-gray-900 font-medium text-sm leading-snug line-clamp-2">{{ note.title }}</h3>
            <svg
              v-if="note.isPinned"
              xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 text-amber-500 shrink-0 mt-0.5" viewBox="0 0 24 24" fill="currentColor"
            >
              <path d="M16.5 3.75a.75.75 0 0 0-.75.75v.75h-7.5V4.5a.75.75 0 0 0-1.5 0v.75H6a.75.75 0 0 0 0 1.5h.75v9.879a3 3 0 0 0 .879 2.122l1.5 1.5a.75.75 0 0 0 1.06 0l1.28-1.28 1.28 1.28a.75.75 0 0 0 1.06 0l1.5-1.5a3 3 0 0 0 .879-2.122V6.75H18a.75.75 0 0 0 0-1.5h-.75V4.5a.75.75 0 0 0-.75-.75Z" />
            </svg>
          </div>

          <p class="text-gray-500 text-xs leading-relaxed line-clamp-4 flex-1 whitespace-pre-line">
            {{ note.content || "No additional content." }}
          </p>

          <div class="flex items-center justify-between mt-4 pt-3 border-t border-gray-100">
            <div class="flex flex-col gap-0.5">
              <span class="text-[11px] text-gray-400">Created {{ formatDate(note.createdAt) }}</span>
              <span v-if="note.updatedAt !== note.createdAt" class="text-[11px] text-gray-400">Edited {{ formatDate(note.updatedAt) }}</span>
            </div>
            <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition">
              <button
                class="p-1.5 rounded-md text-gray-400 hover:text-rose-600 hover:bg-gray-100 transition disabled:opacity-40"
                title="Delete"
                :disabled="deletingId === note.id"
                @click.stop="handleDelete(note)"
              >
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
                </svg>
              </button>
            </div>
          </div>
        </article>
      </div>
    </main>

    <!-- Create / Edit Modal -->
    <div
      v-if="isModalOpen"
      class="fixed inset-0 z-20 flex items-center justify-center bg-black/40 backdrop-blur-sm px-4"
      @click.self="closeModal"
    >
      <div class="w-full max-w-lg bg-white border border-gray-200 rounded-2xl shadow-xl p-6">
        <div class="flex items-center justify-between mb-5">
          <h2 class="text-gray-900 font-semibold text-base">
            {{ modalMode === "create" ? "New note" : "Edit note" }}
          </h2>
          <button class="text-gray-400 hover:text-gray-900 transition" @click="closeModal">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div v-if="editingNoteMeta" class="flex flex-wrap gap-x-4 gap-y-0.5 text-xs text-gray-400 mb-4 -mt-2">
          <span>Created {{ formatDate(editingNoteMeta.createdAt) }}</span>
          <span v-if="editingNoteMeta.updatedAt !== editingNoteMeta.createdAt">Last edited {{ formatDate(editingNoteMeta.updatedAt) }}</span>
        </div>

        <form class="space-y-4" @submit.prevent="submitNote">
          <div>
            <label for="noteTitle" class="block text-sm font-medium text-gray-700 mb-1.5">Title</label>
            <input
              id="noteTitle"
              v-model="noteForm.title"
              type="text"
              class="w-full rounded-lg bg-white border border-gray-300 text-gray-900 placeholder-gray-400 px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 transition"
              placeholder="Note title"
            />
          </div>

          <div>
            <label for="noteContent" class="block text-sm font-medium text-gray-700 mb-1.5">Content</label>
            <textarea
              id="noteContent"
              v-model="noteForm.content"
              rows="6"
              class="w-full rounded-lg bg-white border border-gray-300 text-gray-900 placeholder-gray-400 px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 transition resize-none"
              placeholder="Write something..."
            ></textarea>
          </div>

          <label class="flex items-center gap-2 text-sm text-gray-700 cursor-pointer select-none">
            <input v-model="noteForm.isPinned" type="checkbox" class="rounded border-gray-300 bg-white text-indigo-600 focus:ring-indigo-500" />
            Pin this note
          </label>

          <p v-if="modalError" class="text-sm text-rose-600 bg-rose-50 border border-rose-200 rounded-lg px-3 py-2">
            {{ modalError }}
          </p>

          <div class="flex items-center justify-between gap-2 pt-2">
            <button
              v-if="modalMode === 'edit'"
              type="button"
              :disabled="deletingId === editingNoteId"
              class="inline-flex items-center gap-1.5 text-sm text-rose-600 hover:text-rose-700 disabled:opacity-50 transition"
              @click="handleDeleteFromModal"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
              </svg>
              {{ deletingId === editingNoteId ? "Deleting..." : "Delete note" }}
            </button>
            <div v-else></div>

            <div class="flex items-center gap-2">
              <button
                type="button"
                class="text-sm text-gray-500 hover:text-gray-900 px-4 py-2 transition"
                @click="closeModal"
              >
                Cancel
              </button>
              <button
                type="submit"
                :disabled="isSaving"
                class="inline-flex items-center gap-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-60 text-white font-medium text-sm px-4 py-2 transition"
              >
                <svg v-if="isSaving" class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                </svg>
                {{ modalMode === "create" ? "Create note" : "Save changes" }}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
