<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import {
    getAdminTicketStats,
    getAdminTickets,
    getFilms,
    sortFilmsForDisplay,
    type AdminTicketRow,
    type AdminTicketStats,
    type Film
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
  let tickets: AdminTicketRow[] = [];
  let stats: AdminTicketStats | null = null;
  let filterFilmId = '';
  let filterScreeningId = '';
  let filterDate = '';
  let listLoading = false;
  let statsLoading = false;
  let error = '';

  function buyerLabel(t: AdminTicketRow): string {
    const b = t.buyerType;
    if (b === 0 || b === 'RegisteredUser') return t.userEmail ?? t.userName ?? 'Regisztrált';
    return t.guestEmail ?? t.guestName ?? 'Vendég';
  }

  async function loadFilms() {
    try {
      films = sortFilmsForDisplay(await getFilms());
    } catch {
      films = [];
    }
  }

  async function loadStats() {
    statsLoading = true;
    try {
      stats = await getAdminTicketStats();
    } catch (e) {
      stats = null;
      error = e instanceof Error ? e.message : 'Statisztika hiba.';
    } finally {
      statsLoading = false;
    }
  }

  async function loadTickets() {
    listLoading = true;
    error = '';
    try {
      tickets = await getAdminTickets({
        filmId: filterFilmId || null,
        screeningId: filterScreeningId.trim() || null,
        date: filterDate || null
      });
    } catch (e) {
      tickets = [];
      error = e instanceof Error ? e.message : 'A jegylista nem töltődött be.';
    } finally {
      listLoading = false;
    }
  }

  function formatFt(n: number): string {
    if (!Number.isFinite(n)) return '–';
    return `${Math.round(n).toLocaleString('hu-HU')} Ft`;
  }

  onMount(async () => {
    await loadFilms();
    await Promise.all([loadStats(), loadTickets()]);
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
        <button type="button" class="navbar-link" on:click={() => dispatch('goAdminScreenings')}>Vetítések</button>
        <button type="button" class="navbar-link active" on:click={() => dispatch('goAdminTickets')}>Jegyek</button>
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

  <main class="admin-desk-main">
    {#if !isLoggedIn || !isAdmin}
      <section class="film-form card">
        <h2>Nincs jogosultság</h2>
        <p class="muted">Az összesített jegylistához adminisztrátor szükséges.</p>
        <button type="button" class="save-btn" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      </section>
    {:else}
      <section class="film-form card">
        <h2>Összesített statisztika</h2>
        {#if statsLoading}
          <p class="muted">Statisztika betöltése…</p>
        {:else if stats}
          <div class="admin-stats-grid">
            <div class="admin-stat-card">
              Összbevétel
              <strong>{formatFt(stats.totalRevenue)}</strong>
            </div>
            <div class="admin-stat-card">
              Eladott jegyek
              <strong>{stats.totalTicketsSold}</strong>
            </div>
          </div>
          <h3 class="muted" style="margin: 1rem 0 0.5rem; font-size: 0.95rem;">Top filmek (darab)</h3>
          <ol class="admin-top-movies">
            {#each stats.topMovies as m}
              <li>{m.movieTitle} — {m.ticketsSold} db ({formatFt(m.revenue)})</li>
            {:else}
              <li class="muted">Nincs adat.</li>
            {/each}
          </ol>
          <button type="button" class="save-btn" style="margin-top: 0.75rem;" on:click={loadStats} disabled={statsLoading}>
            Statisztika frissítése
          </button>
        {/if}
      </section>

      <section class="film-form card">
        <h2>Jegyek szűrése</h2>
        <div class="film-form" style="display: grid; gap: 0.75rem; grid-template-columns: 1fr 1fr; max-width: 720px;">
          <label>
            Film
            <select bind:value={filterFilmId}>
              <option value="">Összes film</option>
              {#each films as f}
                <option value={f.id}>{f.title}</option>
              {/each}
            </select>
          </label>
          <label>
            Vetítés azonosító (GUID)
            <input type="text" bind:value={filterScreeningId} placeholder="opcionális" />
          </label>
          <label style="grid-column: 1 / -1;">
            Nap (helyi dátum)
            <input type="date" bind:value={filterDate} />
          </label>
        </div>
        <div class="form-actions" style="margin-top: 0.75rem;">
          <button type="button" class="save-btn" on:click={loadTickets} disabled={listLoading}>
            {listLoading ? 'Betöltés…' : 'Lista frissítése'}
          </button>
        </div>
      </section>

      <section class="film-form card">
        <h2>Jegylista ({tickets.length})</h2>
        {#if listLoading}
          <p class="muted">Betöltés…</p>
        {:else if tickets.length === 0}
          <p class="muted">Nincs találat a szűrésnek megfelelően.</p>
        {:else}
          <div class="admin-table-wrap">
            <table class="admin-table">
              <thead>
                <tr>
                  <th>Film</th>
                  <th>Terem</th>
                  <th>Kezdés</th>
                  <th>Szék</th>
                  <th>Ár</th>
                  <th>Vevő</th>
                  <th>Érvényesítve</th>
                </tr>
              </thead>
              <tbody>
                {#each tickets as t}
                  <tr class:ticket-cancelled={t.isCancelled}>
                    <td>{t.filmTitle}</td>
                    <td>{t.movieHallName}</td>
                    <td>{new Date(t.screeningStartTime).toLocaleString('hu-HU')}</td>
                    <td>{t.seatNumber}</td>
                    <td>{formatFt(t.price)}</td>
                    <td>{buyerLabel(t)}</td>
                    <td>{t.isValidated ? 'igen' : 'nem'}{t.isCancelled ? ' (lemondva)' : ''}</td>
                  </tr>
                {/each}
              </tbody>
            </table>
          </div>
        {/if}
        {#if error}<p class="error">{error}</p>{/if}
      </section>
    {/if}
  </main>
</div>

<style>
  :global(.ticket-cancelled) {
    opacity: 0.55;
  }
</style>
