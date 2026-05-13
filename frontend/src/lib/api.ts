const BASE = 'http://localhost:5244';



let token: string | null =

  typeof localStorage !== 'undefined' ? localStorage.getItem('token') : null;



export function setToken(t: string | null) {

  token = t;

  if (typeof localStorage === 'undefined') return;

  t ? localStorage.setItem('token', t) : localStorage.removeItem('token');

}

export function isAuthenticated(): boolean {
  if (!token) return false;
  const payload = getJwtPayload();
  if (!payload) return false;
  const exp = payload.exp;
  if (typeof exp === 'number' && Number.isFinite(exp)) {
    return Date.now() / 1000 < exp - 15;
  }
  return true;
}

function getJwtPayload(): Record<string, unknown> | null {
  if (!token) return null;
  const parts = token.split('.');
  if (parts.length < 2) return null;

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = atob(base64);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

export function getUserRole(): string | null {
  const payload = getJwtPayload();
  if (!payload) return null;

  const role =
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
    payload.role ??
    payload.Role;

  return typeof role === 'string' ? role : null;
}

export function isAdmin(): boolean {
  return getUserRole() === 'ADMIN';
}

export function isCashier(): boolean {
  return getUserRole() === 'CASHIER';
}

export function getUserEmail(): string | null {
  const payload = getJwtPayload();
  if (!payload) return null;

  const email =
    payload.email ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];

  return typeof email === 'string' ? email : null;
}

export function getUserName(): string | null {
  const payload = getJwtPayload();
  if (!payload) return null;

  const name =
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
    payload.name ??
    payload.unique_name;

  return typeof name === 'string' && name.trim() ? name.trim() : null;
}

export function getUserId(): string | null {
  const payload = getJwtPayload();
  if (!payload) return null;

  const id = payload.sub;
  return typeof id === 'string' ? id : null;
}

/** Admin jegylistából a pénztár nézetbe: a komponens ezt olvassa és törölje. */
export const STAFF_TICKET_PREFILL_STORAGE_KEY = 'jegymester_staff_ticket_prefill_v1';

const UUID_IN_TEXT_RE =
  /\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b/;

/**
 * Kinyeri az első UUID-t beillesztett szövegből (levél, JSON, link).
 * Üres string, ha nincs találat — a hívó jelezhet hibát.
 */
export function extractUuidFromText(raw: string): string {
  const t = raw.trim();
  if (!t) return '';
  const unwrapped = t.replace(/^\{|\}$/g, '').trim();
  const m = unwrapped.match(UUID_IN_TEXT_RE);
  if (m) return m[0];
  const compact = unwrapped.replace(/\s+/g, '');
  const m2 = compact.match(UUID_IN_TEXT_RE);
  return m2 ? m2[0] : '';
}

/**
 * Jegy / felhasználó kereséshez: ha van UUID a szövegben, azt adja vissza, különben trimelt nyers input (pl. tiszta UUID beírás).
 */
export function normalizeStaffTicketLookupInput(raw: string): string {
  const extracted = extractUuidFromText(raw);
  if (extracted) return extracted;
  const single = raw.trim().replace(/^\{|\}$/g, '').trim();
  if (UUID_IN_TEXT_RE.test(single)) return single;
  return raw.trim();
}

function humanizeApiError(body: string): string {

  const t = body.trim().replace(/^["']+|["']+$/g, '');

  const map: Record<string, string> = {
    'For not registered users giving phone number and email is mandatory.':
      'A szerver nem fogadja el a belépésed (néha ez lejárt token). Jelentkezz be újra — vagy töltsd ki az adatokat vendéghez a vetítésnél kosár előtt.',
    'For guest purchases name, phone number and email are mandatory.':
      'Vendég vásárlásnál kötelező a név, az e-mail és a telefonszám minden jegynél.',
    'Ticket already purchased.': 'Erről a vetítésről erre a székre már van eladott jegy.',
    'Invalid User identification.': 'Érvénytelen felhasználó-azonosítás. Jelentkezz be újra.',
    'User does not own that ticket.': 'Ez a jegy nem a fiókodhoz tartozik.',
    'Invalid ticket id.': 'Érvénytelen jegyazonosító.',
    'Movie hall already exists with this name.': 'Ilyen néven már van moziterem.',
    'Another movie hall already uses this name.': 'Ezt a nevet egy másik terem már használja.',
    'Movie hall cannot be removed while screenings are attached.':
      'A termet nem lehet törölni, amíg van hozzá vetítés.',
    'Screening already exists with these parameters.': 'Ilyen vetítés már létezik (film, terem, időpont).',
    'Screening not found.': 'A vetítés nem található.',
    'Ticket cannot be deleted within 4 hours of the screening.':
      'A jegyet a vetítés előtt 4 órán belül nem lehet törölni.',
    'No more tickets can be purchased because the movie hall is full.': 'A terem megtelt, nincs több hely.',
    'This screening has been cancelled.': 'Ezt a vetítést lemondták.'
  };

  return map[t] ?? body;

}



async function req<T>(path: string, opts: RequestInit = {}): Promise<T> {

  const headers = new Headers(opts.headers);

  headers.set('Content-Type', 'application/json');

  if (token) headers.set('Authorization', `Bearer ${token}`);



  let res: Response;

  try {

    res = await fetch(`${BASE}${path}`, { ...opts, headers });

  } catch (e) {

    throw new Error(

      'A szerver nem elérhető.'

    );

  }



  if (!res.ok) {

    const msg = await res.text();

    throw new Error(humanizeApiError(msg) || `Hiba: ${res.status}`);

  }



  return res.json() as Promise<T>;

}



export async function login(email: string, password: string) {

  const out = await req<{ token: string; expiresAt: string }>('/login', {

    method: 'POST',

    body: JSON.stringify({ email, password })

  });

  setToken(out.token);

  return out;

}



export async function register(data: {

  name: string;

  email: string;

  password: string;

  phoneNumber?: string | null;

}) {

  const out = await req<{ token: string; expiresAt: string }>('/register', {

    method: 'POST',

    body: JSON.stringify({

      name: data.name,

      email: data.email,

      password: data.password,

      phoneNumber: data.phoneNumber ?? null

    })

  });

  setToken(out.token);

  return out;

}



export async function getTestNames(): Promise<string[]> {

  return req<string[]>('/');

}

export type Film = {
  id: string;
  title: string;
  description: string;
  length: number;
  ageRating: string;
  releaseDate: string;
  genre: string;
  director: string;
  isActive: boolean;
};

/** Megjelenési dátum szerint csökkenő, majd cím — így a főoldali 10-es szeletben is előkerülnek a frissebb filmek. */
export function sortFilmsForDisplay(films: Film[]): Film[] {
  return [...films].sort((a, b) => {
    const ta = new Date(a.releaseDate).getTime();
    const tb = new Date(b.releaseDate).getTime();
    const da = Number.isFinite(ta) ? ta : 0;
    const db = Number.isFinite(tb) ? tb : 0;
    if (db !== da) return db - da;
    return a.title.localeCompare(b.title, 'hu');
  });
}

export async function getFilms(): Promise<Film[]> {
  return req<Film[]>('/films');
}

export async function getFilm(id: string): Promise<Film> {
  return req<Film>(`/films/${id}`);
}

export type FilmCreateInput = {
  title: string;
  description: string;
  length: number;
  ageRating?: string | null;
  releaseDate: string;
  genre?: string | null;
  director?: string | null;
  isActive?: boolean;
};

export async function createFilm(data: FilmCreateInput): Promise<Film> {
  return req<Film>('/films/admin/upload', {
    method: 'POST',
    body: JSON.stringify({
      title: data.title,
      description: data.description,
      length: data.length,
      ageRating: data.ageRating ?? null,
      releaseDate: data.releaseDate,
      genre: data.genre ?? null,
      director: data.director ?? null,
      isActive: data.isActive ?? true
    })
  });
}

export type FilmUpdateInput = {
  title: string;
  description: string;
  length: number;
  ageRating: string | null;
  releaseDate: string;
  genre: string | null;
  director: string | null;
  isActive: boolean;
};

export async function updateFilm(id: string, data: FilmUpdateInput): Promise<Film> {
  return req<Film>(`/films/admin/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      title: data.title,
      description: data.description,
      length: data.length,
      ageRating: data.ageRating,
      releaseDate: data.releaseDate,
      genre: data.genre,
      director: data.director,
      isActive: data.isActive
    })
  });
}

export async function deleteFilm(id: string): Promise<void> {
  const headers = new Headers();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(`${BASE}/films/admin/${id}`, {
      method: 'DELETE',
      headers
    });
  } catch {
    throw new Error('A szerver nem elérhető.');
  }

  if (!res.ok) {
    const msg = await res.text();
    throw new Error(humanizeApiError(msg) || `Hiba: ${res.status}`);
  }
}

export type Screening = {
  id: string;
  filmId: string;
  filmTitle?: string;
  movieHallId: string;
  /** MovieHalls.HallName a szerverről */
  movieHallName?: string;
  startTime: string;
  basePrice: number;
  isCancelled: boolean;
};

export function screeningHallLabel(s: { movieHallName?: string | null }): string {
  const n = s.movieHallName?.trim();
  return n && n.length > 0 ? n : '—';
}

export async function getScreenings(): Promise<Screening[]> {
  return req<Screening[]>('/screenings');
}

export async function getScreening(id: string): Promise<Screening> {
  return req<Screening>(`/screenings/${id}`);
}

export const TicketBuyerType = {
  RegisteredUser: 0,
  Guest: 1
} as const;

export type PurchaseTicketInput = {
  screeningId: string;
  seatNumber: number;
  buyerType: (typeof TicketBuyerType)[keyof typeof TicketBuyerType];
  userId?: string | null;
  guestName?: string | null;
  guestEmail?: string | null;
  guestPhone?: string | null;
};

export async function purchaseTicket(data: PurchaseTicketInput) {
  return req<{
    screeningId?: string | null;
    seatNumber: number;
    price: number;
    purchasedAt: string;
    userId?: string | null;
    guestName?: string | null;
    guestEmail?: string | null;
    guestPhone?: string | null;
  }>('/tickets/purchase', {
    method: 'POST',
    body: JSON.stringify({
      screeningId: data.screeningId,
      seatNumber: data.seatNumber,
      buyerType: data.buyerType,
      userId: data.userId ?? null,
      guestName: data.guestName ?? null,
      guestEmail: data.guestEmail ?? null,
      guestPhone: data.guestPhone ?? null
    })
  });
}

export type MyTicket = {
  id: string;
  ticketId?: string;
  seatNumber: number;
  price: number;
  purchasedAt: string;
  isCancelled?: boolean;
  isValidated?: boolean;
  validatedAt?: string | null;
  screeningStartTime: string;
  filmTitle: string;
  /** Moziterem neve (TicketResponse / vetítés) */
  movieHallName?: string;
};

function pickStr(row: Record<string, unknown>, camel: string, pascal: string): string {
  const v = row[camel] ?? row[pascal];
  return v == null ? '' : String(v);
}

function pickNum(row: Record<string, unknown>, camel: string, pascal: string, fallback = 0): number {
  const v = row[camel] ?? row[pascal];
  const n = typeof v === 'number' ? v : Number(v);
  return Number.isFinite(n) ? n : fallback;
}

function pickBool(row: Record<string, unknown>, camel: string, pascal: string): boolean {
  const v = row[camel] ?? row[pascal];
  return v === true || v === 'true';
}

function pickMaybeStr(row: Record<string, unknown>, camel: string, pascal: string): string | null {
  const v = row[camel] ?? row[pascal];
  if (v == null || v === '') return null;
  return String(v);
}

function normalizeMyTicket(row: Record<string, unknown>): MyTicket {
  const id =
    pickStr(row, 'ticketId', 'TicketId').trim() ||
    pickStr(row, 'id', 'Id').trim();
  return {
    id,
    ticketId: id,
    seatNumber: pickNum(row, 'seatNumber', 'SeatNumber'),
    price: pickNum(row, 'price', 'Price'),
    purchasedAt: pickStr(row, 'purchasedAt', 'PurchasedAt'),
    isCancelled: pickBool(row, 'isCancelled', 'IsCancelled'),
    isValidated: pickBool(row, 'isValidated', 'IsValidated'),
    validatedAt: pickMaybeStr(row, 'validatedAt', 'ValidatedAt'),
    screeningStartTime: pickStr(row, 'screeningStartTime', 'ScreeningStartTime'),
    filmTitle: pickStr(row, 'filmTitle', 'FilmTitle'),
    movieHallName: pickStr(row, 'movieHallName', 'MovieHallName') || undefined
  };
}

export async function getMyTickets(): Promise<MyTicket[]> {
  const tickets = await req<Record<string, unknown>[]>('/tickets/my-tickets');
  return tickets.map(normalizeMyTicket);
}

export async function cancelMyTicket(ticketId: string): Promise<void> {
  const id = normalizeStaffTicketLookupInput(ticketId);
  if (!id) {
    throw new Error('Nem sikerült felismerni a jegy azonosítóját. Frissítsd a listát, majd próbáld újra.');
  }
  const headers = new Headers();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(`${BASE}/tickets/${encodeURIComponent(id)}`, {
      method: 'DELETE',
      headers
    });
  } catch {
    throw new Error('A szerver nem elérhető.');
  }

  if (!res.ok) {
    const msg = await res.text();
    throw new Error(humanizeApiError(msg) || `Hiba: ${res.status}`);
  }
}

export type MovieHall = {
  id: string;
  hallName: string;
  seatCount: number;
  isOccupied: boolean;
};

export async function getMovieHalls(): Promise<MovieHall[]> {
  return req<MovieHall[]>('/movie-halls');
}

export async function getMovieHall(id: string): Promise<MovieHall> {
  return req<MovieHall>(`/movie-halls/${id}`);
}

export type MovieHallCreateInput = {
  hallName: string;
  seatCount: number;
  isOccupied?: boolean;
};

export async function createMovieHall(data: MovieHallCreateInput): Promise<MovieHall> {
  return req<MovieHall>('/movie-halls/admin/upload', {
    method: 'POST',
    body: JSON.stringify({
      hallName: data.hallName.trim(),
      seatCount: data.seatCount,
      isOccupied: data.isOccupied ?? false
    })
  });
}

export type MovieHallUpdateInput = {
  hallName: string | null;
  seatCount: number | null;
  isOccupied: boolean | null;
};

export async function updateMovieHall(id: string, data: MovieHallUpdateInput): Promise<MovieHall> {
  return req<MovieHall>(`/movie-halls/admin/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      hallName: data.hallName,
      seatCount: data.seatCount,
      isOccupied: data.isOccupied
    })
  });
}

export async function deleteMovieHall(id: string): Promise<void> {
  const headers = new Headers();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(`${BASE}/movie-halls/admin/${id}`, { method: 'DELETE', headers });
  } catch {
    throw new Error('A szerver nem elérhető.');
  }

  if (!res.ok) {
    const msg = await res.text();
    throw new Error(humanizeApiError(msg) || `Hiba: ${res.status}`);
  }
}

export type ScreeningCreateInput = {
  filmId: string;
  movieHallId: string;
  startTime: string;
  basePrice: number;
};

export async function createScreening(data: ScreeningCreateInput): Promise<Screening> {
  return req<Screening>('/screenings/admin/upload', {
    method: 'POST',
    body: JSON.stringify({
      filmId: data.filmId,
      movieHallId: data.movieHallId,
      startTime: data.startTime,
      basePrice: data.basePrice
    })
  });
}

export type ScreeningUpdateInput = {
  filmId: string | null;
  movieHallId: string | null;
  startTime: string | null;
  basePrice: number | null;
  isCancelled: boolean | null;
};

export async function updateScreening(id: string, data: ScreeningUpdateInput): Promise<Screening> {
  return req<Screening>(`/screenings/admin/${id}`, {
    method: 'PUT',
    body: JSON.stringify({
      filmId: data.filmId,
      movieHallId: data.movieHallId,
      startTime: data.startTime,
      basePrice: data.basePrice,
      isCancelled: data.isCancelled
    })
  });
}

export type ScreeningDeleteResult = {
  message: string;
  filmId: string;
  filmTitle: string;
  movieHallId: string;
  movieHallName: string;
};

export async function deleteScreening(id: string): Promise<ScreeningDeleteResult> {
  return req<ScreeningDeleteResult>(`/screenings/admin/${id}`, { method: 'DELETE' });
}

export type AdminTicketRow = {
  ticketId: string;
  screeningId: string;
  filmId: string;
  filmTitle: string;
  movieHallId: string;
  movieHallName: string;
  screeningStartTime: string;
  seatNumber: number;
  price: number;
  purchasedAt: string;
  buyerType: string | number;
  userId?: string | null;
  userName?: string | null;
  userEmail?: string | null;
  guestId?: string | null;
  guestName?: string | null;
  guestEmail?: string | null;
  guestPhone?: string | null;
  isValidated: boolean;
  validatedAt?: string | null;
  isCancelled: boolean;
  cancelledAt?: string | null;
};

function normalizeAdminTicketRow(row: Record<string, unknown>): AdminTicketRow {
  return {
    ticketId: pickStr(row, 'ticketId', 'TicketId'),
    screeningId: pickStr(row, 'screeningId', 'ScreeningId'),
    filmId: pickStr(row, 'filmId', 'FilmId'),
    filmTitle: pickStr(row, 'filmTitle', 'FilmTitle'),
    movieHallId: pickStr(row, 'movieHallId', 'MovieHallId'),
    movieHallName: pickStr(row, 'movieHallName', 'MovieHallName'),
    screeningStartTime: pickStr(row, 'screeningStartTime', 'ScreeningStartTime'),
    seatNumber: pickNum(row, 'seatNumber', 'SeatNumber'),
    price: pickNum(row, 'price', 'Price'),
    purchasedAt: pickStr(row, 'purchasedAt', 'PurchasedAt'),
    buyerType: (row.buyerType ?? row.BuyerType) as string | number,
    userId: pickMaybeStr(row, 'userId', 'UserId'),
    userName: pickMaybeStr(row, 'userName', 'UserName'),
    userEmail: pickMaybeStr(row, 'userEmail', 'UserEmail'),
    guestId: pickMaybeStr(row, 'guestId', 'GuestId'),
    guestName: pickMaybeStr(row, 'guestName', 'GuestName'),
    guestEmail: pickMaybeStr(row, 'guestEmail', 'GuestEmail'),
    guestPhone: pickMaybeStr(row, 'guestPhone', 'GuestPhone'),
    isValidated: pickBool(row, 'isValidated', 'IsValidated'),
    validatedAt: pickMaybeStr(row, 'validatedAt', 'ValidatedAt'),
    isCancelled: pickBool(row, 'isCancelled', 'IsCancelled'),
    cancelledAt: pickMaybeStr(row, 'cancelledAt', 'CancelledAt')
  };
}

export type AdminTicketStats = {
  totalRevenue: number;
  totalTicketsSold: number;
  topMovies: Array<{
    filmId: string;
    movieTitle: string;
    ticketsSold: number;
    revenue: number;
  }>;
  generatedAt: string;
};

export async function getAdminTickets(filters?: {
  filmId?: string | null;
  screeningId?: string | null;
  date?: string | null;
}): Promise<AdminTicketRow[]> {
  const q = new URLSearchParams();
  if (filters?.filmId?.trim()) q.set('filmId', filters.filmId.trim());
  if (filters?.screeningId?.trim()) q.set('screeningId', filters.screeningId.trim());
  if (filters?.date?.trim()) q.set('date', filters.date.trim());
  const qs = q.toString();
  const raw = await req<Record<string, unknown>[]>(`/tickets${qs ? `?${qs}` : ''}`);
  return raw.map(normalizeAdminTicketRow);
}

export async function getAdminTicketStats(): Promise<AdminTicketStats> {
  return req<AdminTicketStats>('/tickets/stats');
}

export async function getTicketByIdForStaff(ticketId: string): Promise<AdminTicketRow> {
  const id = normalizeStaffTicketLookupInput(ticketId);
  if (!id) {
    throw new Error('Érvénytelen jegyazonosító.');
  }
  const row = await req<Record<string, unknown>>(`/tickets/${encodeURIComponent(id)}`);
  return normalizeAdminTicketRow(row);
}

export async function validateTicket(ticketId: string): Promise<void> {
  const id = normalizeStaffTicketLookupInput(ticketId);
  if (!id) {
    throw new Error('Érvénytelen jegyazonosító.');
  }
  const headers = new Headers();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  let res: Response;
  try {
    res = await fetch(`${BASE}/tickets/${encodeURIComponent(id)}/validate`, { method: 'PATCH', headers });
  } catch {
    throw new Error('A szerver nem elérhető.');
  }

  if (!res.ok) {
    const msg = await res.text();
    throw new Error(humanizeApiError(msg) || `Hiba: ${res.status}`);
  }
}

export async function cashierPurchaseTicket(data: PurchaseTicketInput) {
  return req<{
    screeningId?: string | null;
    seatNumber: number;
    price: number;
    purchasedAt: string;
    userId?: string | null;
    guestName?: string | null;
    guestEmail?: string | null;
    guestPhone?: string | null;
  }>('/tickets/cashier/purchase', {
    method: 'POST',
    body: JSON.stringify({
      screeningId: data.screeningId,
      seatNumber: data.seatNumber,
      buyerType: data.buyerType,
      userId: data.userId ?? null,
      guestName: data.guestName ?? null,
      guestEmail: data.guestEmail ?? null,
      guestPhone: data.guestPhone ?? null
    })
  });
}
