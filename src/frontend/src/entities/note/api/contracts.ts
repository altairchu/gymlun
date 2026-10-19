import { type productModel } from '@/entities/product';
import { type MealType } from '../model';

export interface GetNotesRequest {
  date: string;
}

export interface GetNotesResponse {
  notes: NoteItem[];
}

export interface NoteItem {
  id: number;
  date: string;
  /** Time of day it was eaten, `HH:mm:ss` */
  time: string;
  mealType: MealType;
  displayOrder: number;
  productQuantity: number;
  product: Product;
}

export interface Product {
  id: number;
  name: string;
  defaultQuantity: number;
  quantityUnit: productModel.QuantityUnit;
  calories: number;
  protein: number | null;
  fats: number | null;
  carbs: number | null;
  sugar: number | null;
  salt: number | null;
  photoUrl: string | null;
}

export interface GetNotesHistoryRequest {
  from: string;
  to: string;
}

export interface GetNotesHistoryResponse {
  notesHistory: NoteHistoryItem[];
}

export interface NoteHistoryItem {
  date: string;
  caloriesCount: number;
}

export interface NoteRequestBody {
  date: string;
  /** `HH:mm:ss`; the meal is derived from it when `mealType` is left out */
  time?: string;
  mealType?: MealType;
  productId: number;
  productQuantity: number;
  displayOrder?: number;
}

export interface CreateNotesRequestItem {
  productId: number;
  productQuantity: number;
}

export interface CreateNotesRequest {
  date: string;
  /** `HH:mm:ss` */
  time: string;
  items: CreateNotesRequestItem[];
}

export interface CreateNotesResponse {
  ids: number[];
}

export interface UpdateNoteRequest {
  id: number;
  note: NoteRequestBody;
  /** Not sent: the food `note.productId` names, shown before the server confirms the change */
  product?: Product;
}

export interface RecognizeProductItem {
  name: string;
  caloriesCost: number;
  protein: number | null;
  fats: number | null;
  carbs: number | null;
  sugar: number | null;
  salt: number | null;
}

export interface RecognizeNoteItem {
  product: RecognizeProductItem;
  quantity: number;
}

export interface RecognizeNoteResponse {
  notes: RecognizeNoteItem[];
}
