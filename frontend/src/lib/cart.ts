import { writable } from 'svelte/store';

export type CartItem = {
  id: string;
  screeningId: string;
  filmTitle: string;
  screeningStartTime: string;
  movieHallId?: string;
  /** Vetítés API-ból (MovieHalls.HallName) */
  movieHallName?: string | null;
  seatNumber: number;
  ticketPrice: number;
  guestName?: string | null;
  guestEmail?: string | null;
  guestPhone?: string | null;
};

const STORAGE_KEY = 'cart-items';

function readInitialItems(): CartItem[] {
  if (typeof localStorage === 'undefined') return [];

  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return [];

  try {
    const parsed = JSON.parse(raw) as CartItem[];
    if (!Array.isArray(parsed)) return [];
    return parsed;
  } catch {
    return [];
  }
}

function createCartStore() {
  const { subscribe, set, update } = writable<CartItem[]>(readInitialItems());

  subscribe((items) => {
    if (typeof localStorage === 'undefined') return;
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
  });

  return {
    subscribe,
    add(item: Omit<CartItem, 'id'>) {
      const id = crypto.randomUUID();
      update((items) => [...items, { ...item, id }]);
    },
    remove(id: string) {
      update((items) => items.filter((item) => item.id !== id));
    },
    clear() {
      set([]);
    }
  };
}

export const cartStore = createCartStore();
