<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { getFilm, getFilms, type Film } from './lib/api';
  import './styles/Films.css';

  export let isLoggedIn = false;
  export let isAdmin = false;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goFilmEdit: void;
    goScreenings: { filmTitle?: string };
    goProfile: void;
    goCart: void;
  }>();

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

  function formatDuration(minutes: number): string {
    if (!Number.isFinite(minutes) || minutes <= 0) return '-';
    return `${minutes} perc`;
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
      {#if isAdmin}
        <button type="button" class="navbar-link" on:click={() => dispatch('goFilmEdit')}>Admin felület</button>
      {/if}
      {#if isLoggedIn}
        <button type="button" class="navbar-link" on:click={() => dispatch('goProfile')}>Profil</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goCart')}>Kosár</button>
      {:else}
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      {/if}
    </div>
  </nav>

  <main class="films-layout">
    <section class="films-list card">
      <h2>Most műsoron</h2>
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
                <span class="meta-row">
                  <span class="meta-chip">{film.genre || 'Ismeretlen műfaj'}</span>
                  <span class="meta-chip">{formatDuration(film.length)}</span>
                </span>
              </button>
            </li>
          {/each}
        </ul>
      {/if}
    </section>

    <section class="film-details card">
      <h2>Film részletei</h2>
      {#if isDetailLoading}
        <p class="muted">Részletek betöltése...</p>
      {:else if selectedFilm}
        <h3>{selectedFilm.title}</h3>
        <div class="film-badges">
          <span>{selectedFilm.genre || 'Ismeretlen műfaj'}</span>
          <span>{selectedFilm.ageRating || 'N/A'}</span>
          <span>{formatDuration(selectedFilm.length)}</span>
        </div>
        <p class="description">{selectedFilm.description}</p>
        <div class="details-grid">
          <p><strong>Rendező:</strong> {selectedFilm.director}</p>
          <p><strong>Megjelenés:</strong> {formatReleaseDate(selectedFilm.releaseDate)}</p>
        </div>
        <button type="button" class="showtimes-btn" on:click={() => selectedFilm && dispatch('goScreenings', { filmTitle: selectedFilm.title })}>
          Vetítések megtekintése
        </button>
      {:else}
        <p class="muted">Válassz ki egy filmet a listából.</p>
      {/if}
      {#if error}
        <p class="error">{error}</p>
      {/if}
    </section>
  </main>
</div>
