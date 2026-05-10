export const MOVIE_HALL_NAMES: Record<string, string> = {
  '11111111-1111-1111-1111-000000000001': '1. terem'
};

function normId(id: string): string {
  return id.trim().toLowerCase();
}

const BY_ID = Object.fromEntries(
  Object.entries(MOVIE_HALL_NAMES).map(([k, v]) => [normId(k), v.trim()])
);

export function getMovieHallDisplayName(movieHallId: string): string {
  return BY_ID[normId(movieHallId)] ?? movieHallId;
}
