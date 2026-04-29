<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { getFilm, getFilms, type Film } from './lib/api';
  import './styles/Films.css';

  export let isLoggedIn = false;

  const dispatch = createEventDispatcher<{ goLogin: void; goRegister: void; goHome: void; goFilmEdit: void }>();

  let films: Film[] = [];
  let selectedFilm: Film | null = null;
  let isLoading = true;
  let isDetailLoading = false;
  let error = '';

  function formatReleaseDate(date: string): string {
    const parsed = new Date(date);
    if (Number.isNaN(parsed.getTime())) return date;
    return parsed.toLocaleDateString('hu-HU');
  }

  async function loadFilms() {
    isLoading = true;
    error = '';

    try {
      films = await getFilms();
      selectedFilm = films.length > 0 ? films[0] : null;
      if (selectedFilm) {
        await loadFilmDetails(selectedFilm.id, false);
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'Ismeretlen hiba történt.';
    } finally {
      isLoading = false;
    }
  }

  async function loadFilmDetails(id: string, showLoader = true) {
    error = '';
    if (showLoader) {
      isDetailLoading = true;
    }

    try {
      selectedFilm = await getFilm(id);
    } catch (e) {
      error = e instanceof Error ? e.message : 'A film adatai nem tölthetők be.';
    } finally {
      isDetailLoading = false;
    }
  }

  onMount(() => {
    loadFilms();
  });
</script>

<div class="films-page">
  <nav class="navbar">
    <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
    <div class="navbar-menu">
      <button type="button" class="navbar-link">Vetítések</button>
      <button type="button" class="navbar-link active">Filmek</button>
      {#if isLoggedIn}
        <button type="button" class="navbar-link" on:click={() => dispatch('goFilmEdit')}>Filmek módosítása</button>
      {/if}
      <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
    </div>
  </nav>

  <main class="films-layout">
    <section class="films-list card">
      <h2>Elérhető filmek</h2>
      {#if isLoading}
        <p class="muted">Filmek betöltése...</p>
      {:else if films.length === 0}
        <p class="muted">Jelenleg nincs aktív film.</p>
      {:else}
        <ul>
          {#each films as film}
            <li>
              <button
                type="button"
                class:selected={selectedFilm?.id === film.id}
                on:click={() => loadFilmDetails(film.id)}
              >
                <span class="title">{film.title}</span>
                <span class="meta">{film.genre}</span>
              </button>
            </li>
          {/each}
        </ul>
      {/if}
    </section>

    <section class="film-details card">
      <h2>Film adatai</h2>
      {#if isDetailLoading}
        <p class="muted">Részletek betöltése...</p>
      {:else if selectedFilm}
        <h3>{selectedFilm.title}</h3>
        <p class="description">{selectedFilm.description}</p>
        <div class="details-grid">
          <p><strong>Műfaj:</strong> {selectedFilm.genre}</p>
          <p><strong>Rendező:</strong> {selectedFilm.director}</p>
          <p><strong>Korhatár:</strong> {selectedFilm.ageRating}</p>
          <p><strong>Játékidő:</strong> {selectedFilm.length} perc</p>
          <p><strong>Megjelenés:</strong> {formatReleaseDate(selectedFilm.releaseDate)}</p>
        </div>
      {:else}
        <p class="muted">Válassz ki egy filmet a listából.</p>
      {/if}
      {#if error}
        <p class="error">{error}</p>
      {/if}
    </section>
  </main>
</div>
