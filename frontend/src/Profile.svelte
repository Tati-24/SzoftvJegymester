<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { cancelMyTicket, getMyTickets, getUserEmail, getUserRole, type MyTicket } from './lib/api';
  import './styles/Profile.css';

  export let isLoggedIn = false;
  export let isAdmin = false;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goFilmEdit: void;
    goProfile: void;
    goCart: void;
    logout: void;
  }>();

  let email = getUserEmail();
  let role = getUserRole();

  let tickets: MyTicket[] = [];
  let loading = true;
  let error = '';
  let info = '';
  let cancellingTicketId: string | null = null;

  const CANCEL_LEAD_MS = 4 * 60 * 60 * 1000;

  function formatDateTime(value?: string): string {
    if (!value) return '-';
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return value;
    return parsed.toLocaleString('hu-HU');
  }

  function getTicketStatus(ticket: MyTicket): { label: string; cls: string } {
    if (ticket.isCancelled) {
      return { label: 'Lemondott', cls: 'cancelled' };
    }
    if (ticket.isValidated) {
      return { label: 'Validált', cls: 'validated' };
    }
    return { label: 'Aktív', cls: 'active' };
  }

  function getScreeningStartMs(ticket: MyTicket): number | null {
    const iso = ticket.screening?.startTime;
    if (!iso) return null;
    const t = new Date(iso).getTime();
    return Number.isFinite(t) ? t : null;
  }

  function canCancelByPolicy(ticket: MyTicket): boolean {
    if (ticket.isCancelled || ticket.isValidated) return false;
    const startMs = getScreeningStartMs(ticket);
    if (startMs == null) return false;
    const remaining = startMs - Date.now();
    return remaining >= CANCEL_LEAD_MS;
  }

  async function loadMyTickets() {
    if (!isLoggedIn) {
      loading = false;
      return;
    }

    loading = true;
    error = '';
    info = '';
    try {
      tickets = await getMyTickets();
    } catch (e) {
      error = e instanceof Error ? e.message : 'A jegyek betöltése nem sikerült.';
    } finally {
      loading = false;
    }
  }

  $: if (isLoggedIn) {
    email = getUserEmail();
    role = getUserRole();
    void loadMyTickets();
  } else {
    tickets = [];
    loading = false;
    error = '';
  }

  async function handleTicketCancel(ticketId: string) {
    error = '';
    info = '';
    cancellingTicketId = ticketId;

    try {
      await cancelMyTicket(ticketId);
      tickets = tickets.filter((ticket) => ticket.id !== ticketId);
      info = 'A jegy sikeresen lemondva.';
    } catch (e) {
      error = e instanceof Error ? e.message : 'A jegy lemondása nem sikerült.';
    } finally {
      cancellingTicketId = null;
    }
  }

</script>

<div class="profile-page">
  <nav class="navbar">
    <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
    <div class="navbar-menu">
      {#if isAdmin}
        <button type="button" class="navbar-link" on:click={() => dispatch('goFilmEdit')}>Admin felület</button>
      {/if}
      {#if isLoggedIn}
        <button type="button" class="navbar-link active" on:click={() => dispatch('goProfile')}>Profil</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goCart')}>Kosár</button>
      {:else}
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      {/if}
    </div>
  </nav>

  <main class="profile-layout">
    <section class="card">
      <h2>Profil adatok</h2>
      {#if isLoggedIn}
        <p><strong>E-mail:</strong> {email ?? '-'}</p>
        <p><strong>Szerepkör:</strong> {role ?? '-'}</p>
        {#if isAdmin}
          <button type="button" class="primary-btn" on:click={() => dispatch('goFilmEdit')}>Admin felület megnyitása</button>
        {/if}
        <button type="button" class="primary-btn" on:click={() => dispatch('logout')}>Kijelentkezés</button>
      {:else}
        <p class="muted">A profil megtekintéséhez bejelentkezés szükséges.</p>
        <button type="button" class="primary-btn" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      {/if}
    </section>

    <section class="card">
      <h2>Saját jegyek</h2>
      {#if !isLoggedIn}
        <p class="muted">A jegyek megtekintéséhez bejelentkezés szükséges.</p>
      {:else if loading}
        <p class="muted">Jegyek betöltése...</p>
      {:else if tickets.length === 0}
        <p class="muted">Még nincs jegyed.</p>
      {:else}
        <p class="ticket-cancel-policy muted">
          Online csak akkor mondhatod le a jegyet, ha a vetítés kezdetéig legalább <strong>4 óra</strong> van hátra.
        </p>
        <ul class="ticket-list">
          {#each tickets as ticket}
            <li>
              <p><strong>Film:</strong> {ticket.screening?.film?.title ?? 'Ismeretlen film'}</p>
              <p><strong>Időpont:</strong> {formatDateTime(ticket.screening?.startTime)}</p>
              <p><strong>Ülés:</strong> {ticket.seatNumber}</p>
              <p><strong>Ár:</strong> {ticket.ticketPrice} Ft</p>
              <p>
                <strong>Státusz:</strong>
                <span class={`ticket-status ${getTicketStatus(ticket).cls}`}>{getTicketStatus(ticket).label}</span>
              </p>
              {#if ticket.isValidated && ticket.validatedAt}
                <p><strong>Validálva:</strong> {formatDateTime(ticket.validatedAt)}</p>
              {/if}
              <button
                type="button"
                class="danger-btn"
                on:click={() => handleTicketCancel(ticket.id)}
                disabled={cancellingTicketId === ticket.id ||
                  ticket.isCancelled ||
                  !canCancelByPolicy(ticket)}
              >
                {#if cancellingTicketId === ticket.id}
                  Lemondás...
                {:else if ticket.isCancelled}
                  Már lemondva
                {:else if !canCancelByPolicy(ticket)}
                  Nem lemondható online
                {:else}
                  Jegy lemondása
                {/if}
              </button>
            </li>
          {/each}
        </ul>
      {/if}
      {#if info}
        <p class="ok">{info}</p>
      {/if}
      {#if error}
        <p class="error">{error}</p>
      {/if}
    </section>
  </main>
</div>
