<script setup lang="ts">
import { onMounted, onUnmounted, ref } from "vue";
import LoginRegister from "./components/LoginRegister.vue";
import NotesDashboard from "./components/NotesDashboard.vue";
import { clearStoredToken, getStoredToken, setStoredToken } from "./api/axiosInstance";
import type { AuthResponse, User } from "./types";

const currentUser = ref<User | null>(null);
const isBootstrapping = ref(true);

const CURRENT_USER_STORAGE_KEY = "notesapp_user";

function persistSession(payload: AuthResponse) {
  setStoredToken(payload.token);
  localStorage.setItem(CURRENT_USER_STORAGE_KEY, JSON.stringify(payload.user));
  currentUser.value = payload.user;
}

function handleAuthenticated(payload: AuthResponse) {
  persistSession(payload);
}

function handleLogout() {
  clearStoredToken();
  localStorage.removeItem(CURRENT_USER_STORAGE_KEY);
  currentUser.value = null;
}

function handleSessionExpired() {
  handleLogout();
}

onMounted(() => {
  const token = getStoredToken();
  const storedUser = localStorage.getItem(CURRENT_USER_STORAGE_KEY);

  if (token && storedUser) {
    try {
      currentUser.value = JSON.parse(storedUser) as User;
    } catch {
      handleLogout();
    }
  }

  isBootstrapping.value = false;
  window.addEventListener("notesapp:session-expired", handleSessionExpired);
});

onUnmounted(() => {
  window.removeEventListener("notesapp:session-expired", handleSessionExpired);
});
</script>

<template>
  <div v-if="!isBootstrapping">
    <NotesDashboard v-if="currentUser" :current-user="currentUser" @logout="handleLogout" />
    <LoginRegister v-else @authenticated="handleAuthenticated" />
  </div>
</template>
