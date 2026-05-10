<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { getFilms, type Film } from './lib/api';
  import './Home.css';

  export let isLoggedIn = false;
  export let isAdmin = false;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goFilms: void;
    goFilmEdit: void;
    goScreenings: { filmTitle?: string };
    goProfile: void;
    goCart: void;
  }>();

  const demoFilms: Film[] = [
    {
      id: 'demo-1',
      title: 'Üdvözlet a moziban',
      description: 'Ha még nincs film az adatbázisban, így néz ki egy példa.',
      length: 105,
      ageRating: '12',
      releaseDate: '2026-01-01',
      genre: 'Fantasy',
      director: '—',
      isActive: true
    },
    {
      id: 'demo-2',
      title: 'Esti premier',
      description: 'Válaszd a Vetítéseket, ha már van műsor a szerveren.',
      length: 98,
      ageRating: '16',
      releaseDate: '2026-01-15',
      genre: 'Thriller',
      director: '—',
      isActive: true
    },
    {
      id: 'demo-3',
      title: 'Családi matiné',
      description: '…',
      length: 88,
      ageRating: '6',
      releaseDate: '2026-02-01',
      genre: 'Családi',
      director: '—',
      isActive: true
    }
  ];

  let filmsOnDisplay: Film[] = [];
  let loading = true;
  let showingDemo = false;
  let error = '';

  function formatDuration(minutes: number): string {
    if (!Number.isFinite(minutes) || minutes <= 0) return '–';
    return `${minutes} p`;
  }

  async function load() {
    loading = true;
    error = '';
    showingDemo = false;
    try {
      const all = await getFilms();
      const active = all.filter((f) => f.isActive);
      filmsOnDisplay = active.length > 0 ? active.slice(0, 10) : [];
      showingDemo = filmsOnDisplay.length === 0;
      if (showingDemo) {
        filmsOnDisplay = demoFilms;
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'A filmek betöltése nem sikerült.';
      showingDemo = true;
      filmsOnDisplay = demoFilms;
    } finally {
      loading = false;
    }
  }

  onMount(() => {
    load();
  });
</script>

<div class="home-page">
  <nav class="navbar">
    <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
    <div class="navbar-menu">
      <button type="button" class="navbar-link" on:click={() => dispatch('goFilms')}>Filmek</button>
      <button type="button" class="navbar-link" on:click={() => dispatch('goScreenings')}>Vetítések</button>
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

  <main class="home-main">
    <section class="home-films" aria-labelledby="home-films-heading">
      <div class="home-films-head">
        <div>
          <h2 id="home-films-heading">Aktuális kínálat</h2>
          <p class="home-films-sub">Válogass a legújabb kasszasikerek és örök kedvencek közül!</p>
        </div>
        <button type="button" class="home-link-arrow" on:click={() => dispatch('goFilms')}>
          Összes film<span aria-hidden="true"> →</span>
        </button>
      </div>

      {#if showingDemo && !loading}
        <p class="home-demo-note">
          Pillanatnyilag nem érhető el filmlista a szervertől — alább illusztrációk láthatók.
        </p>
      {/if}

      {#if loading}
        <div class="home-skeleton-grid" aria-live="polite">
          {#each Array(4) as _}
            <div class="home-skeleton-card"><div class="home-skel-poster"></div><div class="home-skel-lines"></div></div>
          {/each}
        </div>
      {:else}
        <div class="home-film-scroller">
          <div class="home-film-row">
            {#each filmsOnDisplay as film (film.id)}
              <article class="home-film-tile">
                <div class="home-tile-visual">
                  <span class="home-tile-letter">{film.title.slice(0, 1).toUpperCase()}</span>
                </div>
                <div class="home-tile-body">
                  <h3 class="home-tile-title">{film.title}</h3>
                  <div class="home-tile-stats">
                    <p class="home-tile-stat">Műfaj: {film.genre || '–'}</p>
                    <p class="home-tile-stat">Perc: {formatDuration(film.length)}</p>
                    {#if film.ageRating}
                      <p class="home-tile-stat">Életkor: {film.ageRating}</p>
                    {/if}
                  </div>
                  <button type="button" class="home-tile-link" on:click={() => dispatch('goScreenings', { filmTitle: film.title })}>
                    Időpontok<span class="home-tile-link-chev" aria-hidden="true"> ›</span>
                  </button>
                </div>
              </article>
            {/each}
          </div>
        </div>
      {/if}
    </section>
  </main>

  {#if error && !loading}
    <p class="error home-error-banner">{error}</p>
  {/if}
</div>
