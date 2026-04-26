<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { getFilms, type Film } from './lib/api';
  import './styles/FilmEdit.css';

  export let isLoggedIn = false;

  const dispatch = createEventDispatcher<{ goLogin: void; goRegister: void; goHome: void; goFilms: void }>();

  type FilmForm = {
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

  let films: Film[] = [];
  let isLoading = true;
  let error = '';
  let info = '';

  let form: FilmForm = {
    id: '',
    title: '',
    description: '',
    length: 90,
    ageRating: '12',
    releaseDate: '',
    genre: '',
    director: '',
    isActive: true
  };

  function mapFilmToForm(film: Film): FilmForm {
    return {
      id: film.id,
      title: film.title,
      description: film.description,
      length: film.length,
      ageRating: film.ageRating,
      releaseDate: film.releaseDate.slice(0, 10),
      genre: film.genre,
      director: film.director,
      isActive: film.isActive
    };
  }

  async function loadFilms() {
    isLoading = true;
    error = '';

    try {
      films = await getFilms();
      if (films.length > 0) {
        form = mapFilmToForm(films[0]);
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'Nem sikerült betölteni a filmeket.';
    } finally {
      isLoading = false;
    }
  }

  function selectFilm(id: string) {
    const film = films.find((item) => item.id === id);
    if (!film) return;
    form = mapFilmToForm(film);
    info = '';
    error = '';
  }

  function handleMockSave() {
    info = 'A mentés backendhez még nincs csatlakoztatva. A módosítás lokálisan előkészítve.';
  }

  onMount(() => {
    loadFilms();
  });
</script>

<div class="film-edit-page">
  <nav class="navbar">
    <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
    <div class="navbar-menu">
      <button type="button" class="navbar-link">Vetítések</button>
      <button type="button" class="navbar-link" on:click={() => dispatch('goFilms')}>Filmek</button>
      <button type="button" class="navbar-link active">Filmek módosítása</button>
      <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
    </div>
  </nav>

  <main class="film-edit-layout">
    {#if !isLoggedIn}
      <section class="film-form card">
        <h2>Bejelentkezés szükséges</h2>
        <p class="muted">A filmek módosítása csak bejelentkezett felhasználóknak érhető el.</p>
        <button type="button" class="save-btn" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      </section>
    {:else}
      <section class="film-picker card">
        <h2>Film kiválasztása</h2>
        {#if isLoading}
          <p class="muted">Filmek betöltése...</p>
        {:else if films.length === 0}
          <p class="muted">Nincs módosítható film.</p>
        {:else}
          <select value={form.id} on:change={(e) => selectFilm((e.currentTarget as HTMLSelectElement).value)}>
            {#each films as film}
              <option value={film.id}>{film.title}</option>
            {/each}
          </select>
        {/if}
      </section>

      <section class="film-form card">
        <h2>Film adatok módosítása</h2>
        <form on:submit|preventDefault={handleMockSave}>
          <label>
            Cím
            <input type="text" bind:value={form.title} />
          </label>
          <label>
            Leírás
            <textarea rows="4" bind:value={form.description}></textarea>
          </label>
          <div class="grid">
            <label>
              Játékidő (perc)
              <input type="number" min="1" bind:value={form.length} />
            </label>
            <label>
              Korhatár
              <input type="text" bind:value={form.ageRating} />
            </label>
            <label>
              Megjelenés
              <input type="date" bind:value={form.releaseDate} />
            </label>
            <label>
              Műfaj
              <input type="text" bind:value={form.genre} />
            </label>
            <label>
              Rendező
              <input type="text" bind:value={form.director} />
            </label>
          </div>

          <label class="checkbox">
            <input type="checkbox" bind:checked={form.isActive} />
            Aktív film
          </label>

          <button type="submit" class="save-btn">Módosítás mentése</button>
        </form>

        {#if info}
          <p class="ok">{info}</p>
        {/if}
        {#if error}
          <p class="error">{error}</p>
        {/if}
      </section>
    {/if}
  </main>
</div>
