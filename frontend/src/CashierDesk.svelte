<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import {
    TicketBuyerType,
    cashierPurchaseTicket,
    getScreenings,
    getTicketByIdForStaff,
    normalizeStaffTicketLookupInput,
    screeningHallLabel,
    STAFF_TICKET_PREFILL_STORAGE_KEY,
    validateTicket,
    type AdminTicketRow,
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

  const canLookupTicket = isLoggedIn && (isCashier || isAdmin);

  let ticketIdInput = '';
  let loaded: AdminTicketRow | null = null;
  let lookupLoading = false;
  let lookupError = '';
  let lookupInfo = '';

  let validateLoading = false;

  let screenings: Screening[] = [];
  let screeningsLoading = false;
  let purchaseScreeningId = '';
  let purchaseSeat = 1;
  let purchaseBuyer: 'reg' | 'guest' = 'guest';
  let purchaseUserId = '';
  let purchaseGuestName = '';
  let purchaseGuestEmail = '';
  let purchaseGuestPhone = '';
  let purchaseLoading = false;
  let purchaseError = '';
  let purchaseInfo = '';
  let copyLookupHint = '';

  async function loadScreeningsList() {
    if (!isCashier) return;
    screeningsLoading = true;
    purchaseError = '';
    try {
      screenings = await getScreenings();
      if (!purchaseScreeningId && screenings[0]) purchaseScreeningId = screenings[0].id;
    } catch (e) {
      screenings = [];
      purchaseError = e instanceof Error ? e.message : 'Vetítések betöltése sikertelen.';
    } finally {
      screeningsLoading = false;
    }
  }

  onMount(() => {
    if (isCashier && isLoggedIn) loadScreeningsList();
    queueMicrotask(() => applyPrefillFromAdmin());
  });

  function applyPrefillFromAdmin() {
    if (typeof sessionStorage === 'undefined' || !canLookupTicket) return;
    const v = sessionStorage.getItem(STAFF_TICKET_PREFILL_STORAGE_KEY);
    if (!v?.trim()) return;
    sessionStorage.removeItem(STAFF_TICKET_PREFILL_STORAGE_KEY);
    ticketIdInput = v.trim();
    void handleLookup();
  }

  async function copyTicketRef(text: string) {
    copyLookupHint = '';
    try {
      await navigator.clipboard.writeText(text);
      copyLookupHint = 'A hivatkozás a vágólapra került.';
      setTimeout(() => (copyLookupHint = ''), 2500);
    } catch {
      copyLookupHint = 'A másolás nem sikerült ebben a böngészőben.';
      setTimeout(() => (copyLookupHint = ''), 3500);
    }
  }

  async function handleLookup() {
    lookupError = '';
    lookupInfo = '';
    loaded = null;
    const id = normalizeStaffTicketLookupInput(ticketIdInput);
    if (!id) {
      lookupError = 'Illeszd be a visszaigazolás szövegét, vagy írd be a jegyhez tartozó belső kódot.';
      return;
    }
    if (!canLookupTicket) {
      lookupError = 'Jegy lekérdezéshez admin vagy pénztáros fiók szükséges.';
      return;
    }
    lookupLoading = true;
    try {
      loaded = await getTicketByIdForStaff(id);
      lookupInfo = 'Jegy betöltve.';
    } catch (e) {
      lookupError = e instanceof Error ? e.message : 'A jegy nem található.';
    } finally {
      lookupLoading = false;
    }
  }

  async function handleValidate() {
    if (!isCashier || !loaded) return;
    lookupError = '';
    lookupInfo = '';
    validateLoading = true;
    try {
      await validateTicket(loaded.ticketId);
      lookupInfo = 'Jegy érvényesítve.';
      loaded = await getTicketByIdForStaff(loaded.ticketId);
    } catch (e) {
      lookupError = e instanceof Error ? e.message : 'Érvényesítés sikertelen.';
    } finally {
      validateLoading = false;
    }
  }

  async function handleCashierPurchase() {
    purchaseError = '';
    purchaseInfo = '';
    if (!isCashier) return;
    if (!purchaseScreeningId) {
      purchaseError = 'Válassz vetítést.';
      return;
    }
    if (!Number.isInteger(purchaseSeat) || purchaseSeat < 1) {
      purchaseError = 'Érvényes székszám szükséges.';
      return;
    }
    if (purchaseBuyer === 'reg') {
      const uid = normalizeStaffTicketLookupInput(purchaseUserId);
      if (!uid) {
        purchaseError =
          'Regisztrált vevőhöz szükség van a vevő fiókjára mutató belső kódra — illeszd be a teljes visszaigazolást, vagy a kódot, ha megvan.';
        return;
      }
      purchaseUserId = uid;
    } else {
      if (!purchaseGuestName.trim() || !purchaseGuestEmail.trim() || !purchaseGuestPhone.trim()) {
        purchaseError = 'Vendég vásárlásnál név, e-mail és telefon kötelező.';
        return;
      }
    }

    purchaseLoading = true;
    try {
      await cashierPurchaseTicket({
        screeningId: purchaseScreeningId,
        seatNumber: purchaseSeat,
        buyerType: purchaseBuyer === 'reg' ? TicketBuyerType.RegisteredUser : TicketBuyerType.Guest,
        userId: purchaseBuyer === 'reg' ? purchaseUserId.trim() : null,
        guestName: purchaseBuyer === 'guest' ? purchaseGuestName.trim() : null,
        guestEmail: purchaseBuyer === 'guest' ? purchaseGuestEmail.trim() : null,
        guestPhone: purchaseBuyer === 'guest' ? purchaseGuestPhone.trim() : null
      });
      const scr = screenings.find((x) => x.id === purchaseScreeningId);
      const hallLabel = scr ? screeningHallLabel(scr) : '';
      purchaseInfo = hallLabel
        ? `Jegy eladva (azonnal érvényesítve a pénztári csatornán). Terem: ${hallLabel}.`
        : 'Jegy eladva (azonnal érvényesítve a pénztári csatornán).';
      purchaseSeat = purchaseSeat + 1;
      await loadScreeningsList();
    } catch (e) {
      purchaseError = e instanceof Error ? e.message : 'Eladás sikertelen.';
    } finally {
      purchaseLoading = false;
    }
  }
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
        <button type="button" class="navbar-link" on:click={() => dispatch('goAdminTickets')}>Jegyek</button>
      {/if}
      {#if isCashier || isAdmin}
        <button type="button" class="navbar-link active" on:click={() => dispatch('goCashier')}>Pénztár</button>
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
    {#if !isLoggedIn}
      <section class="film-form card">
        <h2>Bejelentkezés szükséges</h2>
        <p class="muted">A pénztár funkciókhoz jelentkezz be pénztáros vagy admin fiókkal.</p>
        <button type="button" class="save-btn" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      </section>
    {:else}
      <section class="film-form card">
        <h2>Jegy keresése</h2>
        <p class="muted">
          Admin és pénztáros megtekintheti a részleteket; érvényesítés csak pénztárosnak engedélyezett. A visszaigazolás teljes
          szövegét is beillesztheted — a rendszer kikeresi belőle a jegyet.
        </p>
        <div class="film-form" style="display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: flex-end;">
          <label style="flex: 1; min-width: 220px;">
            Jegy adatai vagy visszaigazolás
            <textarea
              rows="3"
              bind:value={ticketIdInput}
              placeholder="Pl. illeszd be az e-mail szövegét, vagy a jegyhez kapott technikai sort…"
              class="cashier-paste-field"
            ></textarea>
          </label>
          <button type="button" class="save-btn" on:click={handleLookup} disabled={lookupLoading || !canLookupTicket}>
            {lookupLoading ? 'Keresés…' : 'Jegy betöltése'}
          </button>
        </div>
        {#if !canLookupTicket}
          <p class="error">Ehhez a művelethez admin vagy pénztáros jogkör kell.</p>
        {/if}
        {#if lookupInfo}<p class="ok">{lookupInfo}</p>{/if}
        {#if lookupError}<p class="error">{lookupError}</p>{/if}

        {#if loaded}
          <div class="cashier-ticket-detail">
            <p class="cashier-ticket-headline">
              <strong>{loaded.filmTitle}</strong>
              · {new Date(loaded.screeningStartTime).toLocaleString('hu-HU')}
              · {loaded.movieHallName}
              · <strong>szék {loaded.seatNumber}</strong>
            </p>
            <p><strong>Ár:</strong> {loaded.price} Ft</p>
            <p><strong>Érvényesítve:</strong> {loaded.isValidated ? `igen (${loaded.validatedAt ? new Date(loaded.validatedAt).toLocaleString('hu-HU') : ''})` : 'nem'}</p>
            <p><strong>Lemondva:</strong> {loaded.isCancelled ? 'igen' : 'nem'}</p>
            <p class="muted cashier-internal-ref">
              <span>Belső hivatkozás (csak ügyintézéshez):</span>
              <code class="ticket-ref-code">{loaded.ticketId}</code>
              <button type="button" class="linkish-btn" on:click={() => { const t = loaded; if (t) void copyTicketRef(t.ticketId); }}>Másolás</button>
            </p>
            {#if copyLookupHint}<p class="ok small-hint">{copyLookupHint}</p>{/if}
          </div>
          {#if isCashier && loaded && !loaded.isValidated && !loaded.isCancelled}
            <button type="button" class="save-btn" on:click={handleValidate} disabled={validateLoading}>
              {validateLoading ? 'Érvényesítés…' : 'Jegy érvényesítése'}
            </button>
          {/if}
        {/if}
      </section>

      {#if isCashier}
        <section class="film-form card">
          <h2>Helyszíni eladás (pénztár)</h2>
          <p class="muted">A jegy azonnal érvényesítve kerül rögzítésre.</p>
          {#if screeningsLoading}
            <p class="muted">Vetítések betöltése…</p>
          {:else}
            <form
              on:submit|preventDefault={handleCashierPurchase}
              class="film-form"
              style="display: flex; flex-direction: column; gap: 0.85rem; max-width: 480px;"
            >
              <label>
                Vetítés
                <select bind:value={purchaseScreeningId} disabled={purchaseLoading}>
                  {#each screenings as s}
                    <option value={s.id}>
                      {s.filmTitle ?? 'Film'} — {new Date(s.startTime).toLocaleString('hu-HU')} ({s.basePrice} Ft)
                    </option>
                  {:else}
                    <option value="">Nincs aktív vetítés</option>
                  {/each}
                </select>
              </label>
              <label>
                Szék száma
                <input type="number" min="1" step="1" bind:value={purchaseSeat} disabled={purchaseLoading} />
              </label>
              <label>
                Vevő típusa
                <select bind:value={purchaseBuyer} disabled={purchaseLoading}>
                  <option value="guest">Vendég</option>
                  <option value="reg">Regisztrált vevő (fiók)</option>
                </select>
              </label>
              {#if purchaseBuyer === 'reg'}
                <label>
                  Vevő fiókja
                  <textarea
                    rows="2"
                    bind:value={purchaseUserId}
                    disabled={purchaseLoading}
                    placeholder="Illeszd be a vevőhöz tartozó visszaigazolást vagy belső kódot…"
                    class="cashier-paste-field"
                  ></textarea>
                </label>
                <p class="muted small-hint">
                  A szerver a vevő belső fiókazonosítóját várja — ezt gyakran egy szövegben kapod meg; beillesztés után a
                  rendszer kiszedi.
                </p>
              {:else}
                <label>
                  Vendég neve
                  <input type="text" bind:value={purchaseGuestName} disabled={purchaseLoading} />
                </label>
                <label>
                  E-mail
                  <input type="email" bind:value={purchaseGuestEmail} disabled={purchaseLoading} />
                </label>
                <label>
                  Telefon
                  <input type="tel" bind:value={purchaseGuestPhone} disabled={purchaseLoading} />
                </label>
              {/if}
              <button type="submit" class="save-btn" disabled={purchaseLoading || screenings.length === 0}>
                {purchaseLoading ? 'Eladás…' : 'Jegy eladása'}
              </button>
            </form>
          {/if}
          {#if purchaseInfo}<p class="ok">{purchaseInfo}</p>{/if}
          {#if purchaseError}<p class="error">{purchaseError}</p>{/if}
        </section>
      {:else if isAdmin}
        <section class="film-form card">
          <h2>Pénztári eladás</h2>
          <p class="muted">A helyszíni eladás csak pénztáros szerepkörrel érhető el. Adminként a jegy lekérdezés és az admin jegylisták használhatók.</p>
        </section>
      {/if}
    {/if}
  </main>
</div>

<style>
  .cashier-ticket-detail {
    margin: 1rem 0;
    padding: 0.75rem 0;
    border-top: 1px solid var(--border);
  }
  .cashier-ticket-detail p {
    margin: 0.35rem 0;
  }
  .cashier-ticket-headline {
    font-size: 1.05rem;
    line-height: 1.45;
  }
  .cashier-internal-ref {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.35rem 0.5rem;
    margin-top: 0.75rem !important;
    font-size: 0.85rem;
  }
  .ticket-ref-code {
    font-size: 0.8rem;
    word-break: break-all;
    background: var(--surface-muted, rgba(0, 0, 0, 0.06));
    padding: 0.15rem 0.35rem;
    border-radius: 4px;
  }
  .linkish-btn {
    border: none;
    background: none;
    color: var(--accent, #2563eb);
    cursor: pointer;
    text-decoration: underline;
    font: inherit;
    padding: 0;
  }
  .linkish-btn:hover {
    opacity: 0.85;
  }
  .cashier-paste-field {
    width: 100%;
    resize: vertical;
    min-height: 2.5rem;
    font: inherit;
  }
  .small-hint {
    font-size: 0.88rem;
    margin: 0.25rem 0 0;
  }
</style>
