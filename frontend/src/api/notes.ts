import { axiosInstance } from "./axiosInstance";
import type { CreateNotePayload, Note, UpdateNotePayload } from "../types";

export async function fetchNotes(): Promise<Note[]> {
  const { data } = await axiosInstance.get<Note[]>("/notes");
  return data;
}

export async function createNote(payload: CreateNotePayload): Promise<Note> {
  const { data } = await axiosInstance.post<Note>("/notes", payload);
  return data;
}

export async function updateNote(id: string, payload: UpdateNotePayload): Promise<Note> {
  const { data } = await axiosInstance.put<Note>(`/notes/${id}`, payload);
  return data;
}

export async function deleteNote(id: string): Promise<void> {
  await axiosInstance.delete(`/notes/${id}`);
}
