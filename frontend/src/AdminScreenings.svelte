<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import {
    createScreening,
    deleteScreening,
    getFilms,
    getMovieHalls,
    getScreenings,
    sortFilmsForDisplay,
    updateScreening,
    type Film,
    type MovieHall,
    type Screening
  } from './lib/api';
  import NavbarBackToHome from './NavbarBackToHome.svelte';
  import './styles/FilmEdit.css';
  import './styles/AdminDesk.css';

  export let isLoggedIn = false;
  export let isAdmin = false;
  export let isCashier = false;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goFilmEdit: void;
    goAdminHalls: void;
    goAdminScreenings: void;
    goAdminTickets: void;
    goCashier: void;
    goProfile: void;
    goCart: void;
    logout: void;
  }>();

  let films: Film[] = [];
  let halls: MovieHall[] = [];
  let screenings: Screening[] = [];
  let isLoading = true;
  let saveLoading = false;
  let deleteLoading = false;
  let error = '';
  let info = '';
  let isNewMode = false;

  let formId = '';
  let filmId = '';
  let movieHallId = '';
  let startLocal = '';
  let basePrice = 1800;
  let isCancelled = false;

  function toLocalInput(iso: string): string {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';
    const p = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}`;
  }

  function emptyForm() {
    formId = '';
    filmId = films[0]?.id ?? '';
    movieHallId = halls[0]?.id ?? '';
    startLocal = '';
    basePrice = 1800;
    isCancelled = false;
    isNewMode = true;
  }

  function mapScreening(s: Screening) {
    formId = s.id;
    filmId = s.filmId;
    movieHallId = s.movieHallId;
    startLocal = toLocalInput(s.startTime);
    basePrice = Number(s.basePrice);
    isCancelled = s.isCancelled;
    isNewMode = false;
  }

  async function loadAll() {
    isLoading = true;
    error = '';
    try {
      const [f, h, sc] = await Promise.all([getFilms(), getMovieHalls(), getScreenings()]);
      films = sortFilmsForDisplay(f);
      halls = h;
      screenings = [...sc].sort(
        (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime()
      );
      if (!filmId && films[0]) filmId = films[0].id;
      if (!movieHallId && halls[0]) movieHallId = halls[0].id;
      if (isNewMode) {
        if (!startLocal) {
          const d = new Date();
          d.setHours(d.getHours() + 2, 0, 0, 0);
          startLocal = toLocalInput(d.toISOString());
        }
      } else if (formId) {
        const cur = screenings.find((x) => x.id === formId);
        if (cur) mapScreening(cur);
        else if (screenings.length) mapScreening(screenings[0]);
        else emptyForm();
      } else if (screenings.length) {
        mapScreening(screenings[0]);
      } else {
        emptyForm();
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'Betöltési hiba.';
    } finally {
      isLoading = false;
    }
  }

  function selectFromList(id: string) {
    const s = screenings.find((x) => x.id === id);
    if (s) mapScreening(s);
  }

  function validateForm(): string | null {
    if (!filmId) return 'Válassz filmet.';
    if (!movieHallId) return 'Válassz termet.';
    if (!startLocal) return 'Add meg az időpontot.';
    const t = new Date(startLocal).getTime();
    if (!Number.isFinite(t)) return 'Érvénytelen időpont.';
    if (!Number.isFinite(basePrice) || basePrice < 0) return 'Érvénytelen ár.';
    return null;
  }

  async function handleSave() {
    info = '';
    error = '';
    const v = validateForm();
    if (v) {
      error = v;
      return;
    }
    const startIso = new Date(startLocal).toISOString();
    saveLoading = true;
    try {
      if (isNewMode) {
        const created = await createScreening({
          filmId,
          movieHallId,
          startTime: startIso,
          basePrice
        });
        info = 'Vetítés létrehozva.';
        isNewMode = false;
        await loadAll();
        mapScreening(created);
      } else {
        const updated = await updateScreening(formId, {
          filmId,
          movieHallId,
          startTime: startIso,
          basePrice,
          isCancelled
        });
        info = 'Vetítés frissítve.';
        await loadAll();
        mapScreening(updated);
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'A mentés nem sikerült.';
    } finally {
      saveLoading = false;
    }
  }

  async function handleDelete() {
    if (isNewMode || !formId) return;
    if (!confirm('Biztosan törlöd ezt a vetítést? A kapcsolódó jegyek is törlődnek.')) return;
    info = '';
    error = '';
    deleteLoading = true;
    try {
      await deleteScreening(formId);
      info = 'Vetítés törölve.';
      await loadAll();
      if (screenings.length) mapScreening(screenings[0]);
      else emptyForm();
    } catch (e) {
      error = e instanceof Error ? e.message : 'A törlés nem sikerült.';
    } finally {
      deleteLoading = false;
    }
  }

  onMount(() => {
    loadAll();
  });
</script>

<div class="film-edit-page">
  <nav class="navbar">
    <div class="navbar-brand-group">
      <NavbarBackToHome on:goHome={() => dispatch('goHome')} />
      <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
    </div>
    <div class="navbar-menu admin-nav-row">
      {#if isAdmin}
        <button type="button" class="navbar-link" on:click={() => dispatch('goFilmEdit')}>Filmek</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goAdminHalls')}>Mozitermek</button>
        <button type="button" class="navbar-link active" on:click={() => dispatch('goAdminScreenings')}>Vetítések</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goAdminTickets')}>Jegyek</button>
      {/if}
      {#if isCashier || isAdmin}
        <button type="button" class="navbar-link" on:click={() => dispatch('goCashier')}>Pénztár</button>
      {/if}
      {#if isLoggedIn}
        <button type="button" class="navbar-link" on:click={() => dispatch('goProfile')}>Profil</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goCart')}>Kosár</button>
        <button type="button" class="navbar-link navbar-link-logout" on:click={() => dispatch('logout')}>Kijelentkezés</button>
      {:else}
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      {/if}
    </div>
  </nav>

  <main class="admin-desk-grid">
    {#if !isLoggedIn || !isAdmin}
      <section class="film-form card" style="grid-column: 1 / -1;">
        <h2>Nincs jogosultság</h2>
        <p class="muted">A vetítések kezelése csak adminisztrátornak érhető el.</p>
        <button type="button" class="save-btn" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      </section>
    {:else}
      <section class="film-picker card admin-tools">
        <h2>Aktív vetítések</h2>
        {#if isLoading}
          <p class="muted">Betöltés…</p>
        {:else if screenings.length === 0}
          <p class="muted">Nincs aktív vetítés — hozz létre egyet.</p>
          <button type="button" class="picker-new-btn" on:click={emptyForm}>Új vetítés</button>
        {:else}
          <ul class="admin-screening-pick">
            {#each screenings as s}
              <li>
                <button
                  type="button"
                  class:selected={!isNewMode && formId === s.id}
                  class="admin-screening-pick-btn"
                  on:click={() => selectFromList(s.id)}
                >
                  <strong>{s.filmTitle ?? 'Film'}</strong>
                  <span class="muted">{new Date(s.startTime).toLocaleString('hu-HU')}</span>
                </button>
              </li>
            {/each}
          </ul>
          <button type="button" class="picker-new-btn" on:click={emptyForm} disabled={saveLoading || deleteLoading}>
            Új vetítés űrlap
          </button>
        {/if}
      </section>

      <section class="film-form card">
        <h2>{isNewMode ? 'Új vetítés' : 'Vetítés szerkesztése'}</h2>
        <form on:submit|preventDefault={handleSave}>
          <label>
            Film
            <select bind:value={filmId} disabled={saveLoading || deleteLoading || films.length === 0}>
              {#each films as f}
                <option value={f.id}>{f.title}</option>
              {/each}
            </select>
          </label>
          <label>
            Terem
            <select bind:value={movieHallId} disabled={saveLoading || deleteLoading || halls.length === 0}>
              {#each halls as h}
                <option value={h.id}>{h.hallName} ({h.seatCount} szék)</option>
              {/each}
            </select>
          </label>
          <label>
            Kezdés (helyi idő)
            <input type="datetime-local" bind:value={startLocal} required disabled={saveLoading || deleteLoading} />
          </label>
          <label>
            Alapár (Ft)
            <input type="number" min="0" step="1" bind:value={basePrice} required disabled={saveLoading || deleteLoading} />
          </label>
          {#if !isNewMode}
            <label class="checkbox">
              <input type="checkbox" bind:checked={isCancelled} disabled={saveLoading || deleteLoading} />
              Vetítés lemondva
            </label>
          {/if}
          <div class="form-actions">
            <button type="submit" class="save-btn" disabled={saveLoading || deleteLoading}>
              {saveLoading ? 'Mentés…' : isNewMode ? 'Vetítés létrehozása' : 'Mentés'}
            </button>
            <button
              type="button"
              class="delete-btn"
              on:click={handleDelete}
              disabled={saveLoading || deleteLoading || isNewMode || !formId}
            >
              {deleteLoading ? 'Törlés…' : 'Vetítés törlése'}
            </button>
          </div>
        </form>
        {#if info}<p class="ok">{info}</p>{/if}
        {#if error}<p class="error">{error}</p>{/if}
      </section>
    {/if}
  </main>
</div>

<style>
  .admin-screening-pick {
    list-style: none;
    margin: 0 0 0.75rem;
    padding: 0;
    max-height: 280px;
    overflow-y: auto;
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
  }
  .admin-screening-pick-btn {
    width: 100%;
    text-align: left;
    padding: 0.5rem 0.65rem;
    border-radius: 8px;
    border: 1px solid var(--border);
    background: var(--input-bg);
    color: var(--text);
    cursor: pointer;
    font: inherit;
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
  }
  .admin-screening-pick-btn.selected {
    border-color: var(--red);
    box-shadow: 0 0 0 1px var(--red);
  }
  .admin-screening-pick-btn:hover {
    filter: brightness(1.08);
  }
</style>
