<script setup lang="ts">
import { computed, reactive, ref } from "vue";
import { loginUser, registerUser } from "../api/auth";
import type { AuthResponse } from "../types";

const emit = defineEmits<{
  (e: "authenticated", payload: AuthResponse): void;
}>();

type Mode = "login" | "register";

const mode = ref<Mode>("login");
const isSubmitting = ref(false);
const serverError = ref("");

const form = reactive({
  email: "",
  displayName: "",
  password: "",
  confirmPassword: "",
});

const fieldErrors = reactive({
  email: "",
  displayName: "",
  password: "",
  confirmPassword: "",
});

const isRegister = computed(() => mode.value === "register");

function resetFieldErrors() {
  fieldErrors.email = "";
  fieldErrors.displayName = "";
  fieldErrors.password = "";
  fieldErrors.confirmPassword = "";
}

function validate(): boolean {
  resetFieldErrors();
  serverError.value = "";
  let valid = true;

  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailPattern.test(form.email)) {
    fieldErrors.email = "Enter a valid email address.";
    valid = false;
  }

  if (isRegister.value && form.displayName.trim().length < 2) {
    fieldErrors.displayName = "Display name must be at least 2 characters.";
    valid = false;
  }

  if (form.password.length < 8) {
    fieldErrors.password = "Password must be at least 8 characters.";
    valid = false;
  }

  if (isRegister.value && form.password !== form.confirmPassword) {
    fieldErrors.confirmPassword = "Passwords do not match.";
    valid = false;
  }

  return valid;
}

function toggleMode() {
  mode.value = isRegister.value ? "login" : "register";
  resetFieldErrors();
  serverError.value = "";
}

async function handleSubmit() {
  if (!validate()) return;

  isSubmitting.value = true;
  serverError.value = "";

  try {
    const response = isRegister.value
      ? await registerUser({
          email: form.email.trim(),
          displayName: form.displayName.trim(),
          password: form.password,
        })
      : await loginUser({
          email: form.email.trim(),
          password: form.password,
        });

    emit("authenticated", response);
  } catch (err) {
    serverError.value = err instanceof Error ? err.message : "Something went wrong.";
  } finally {
    isSubmitting.value = false;
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 px-4">
    <div class="w-full max-w-md">
      <div class="text-center mb-8">
        <h1 class="text-2xl font-semibold text-gray-900 tracking-tight">Techbodia Notes</h1>
        <p class="text-gray-500 text-sm mt-1">
          {{ isRegister ? "Create an account to get started." : "Sign in to your notebook." }}
        </p>
      </div>

      <div class="bg-white border border-gray-200 rounded-2xl shadow-sm p-6 sm:p-8">
        <form class="space-y-5" @submit.prevent="handleSubmit" novalidate>
          <div v-if="isRegister">
            <label for="displayName" class="block text-sm font-medium text-gray-700 mb-1.5">Display name</label>
            <input
              id="displayName"
              v-model="form.displayName"
              type="text"
              autocomplete="name"
              class="w-full rounded-lg bg-white border border-gray-300 text-gray-900 placeholder-gray-400 px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
              :class="{ 'ring-2 ring-rose-500': fieldErrors.displayName }"
              placeholder="Jane Doe"
            />
            <p v-if="fieldErrors.displayName" class="mt-1.5 text-xs text-rose-600">{{ fieldErrors.displayName }}</p>
          </div>

          <div>
            <label for="email" class="block text-sm font-medium text-gray-700 mb-1.5">Email</label>
            <input
              id="email"
              v-model="form.email"
              type="email"
              autocomplete="email"
              class="w-full rounded-lg bg-white border border-gray-300 text-gray-900 placeholder-gray-400 px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
              :class="{ 'ring-2 ring-rose-500': fieldErrors.email }"
              placeholder="you@example.com"
            />
            <p v-if="fieldErrors.email" class="mt-1.5 text-xs text-rose-600">{{ fieldErrors.email }}</p>
          </div>

          <div>
            <label for="password" class="block text-sm font-medium text-gray-700 mb-1.5">Password</label>
            <input
              id="password"
              v-model="form.password"
              type="password"
              :autocomplete="isRegister ? 'new-password' : 'current-password'"
              class="w-full rounded-lg bg-white border border-gray-300 text-gray-900 placeholder-gray-400 px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
              :class="{ 'ring-2 ring-rose-500': fieldErrors.password }"
              placeholder="••••••••"
            />
            <p v-if="fieldErrors.password" class="mt-1.5 text-xs text-rose-600">{{ fieldErrors.password }}</p>
          </div>

          <div v-if="isRegister">
            <label for="confirmPassword" class="block text-sm font-medium text-gray-700 mb-1.5">Confirm password</label>
            <input
              id="confirmPassword"
              v-model="form.confirmPassword"
              type="password"
              autocomplete="new-password"
              class="w-full rounded-lg bg-white border border-gray-300 text-gray-900 placeholder-gray-400 px-3.5 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition"
              :class="{ 'ring-2 ring-rose-500': fieldErrors.confirmPassword }"
              placeholder="••••••••"
            />
            <p v-if="fieldErrors.confirmPassword" class="mt-1.5 text-xs text-rose-600">{{ fieldErrors.confirmPassword }}</p>
          </div>

          <p v-if="serverError" class="text-sm text-rose-600 bg-rose-50 border border-rose-200 rounded-lg px-3 py-2">
            {{ serverError }}
          </p>

          <button
            type="submit"
            :disabled="isSubmitting"
            class="w-full flex items-center justify-center gap-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-60 disabled:cursor-not-allowed text-white font-medium text-sm py-2.5 transition shadow-sm"
          >
            <svg v-if="isSubmitting" class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            {{ isRegister ? "Create account" : "Sign in" }}
          </button>
        </form>

        <p class="text-center text-sm text-gray-500 mt-6">
          {{ isRegister ? "Already have an account?" : "Don't have an account?" }}
          <button type="button" class="text-indigo-600 hover:text-indigo-500 font-medium ml-1" @click="toggleMode">
            {{ isRegister ? "Sign in" : "Register" }}
          </button>
        </p>
      </div>
    </div>
  </div>
</template>
