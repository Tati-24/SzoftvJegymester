<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import { getFilm, getScreening, getScreenings, type Screening } from './lib/api';
  import { cartStore } from './lib/cart';
  import { getMovieHallDisplayName } from './lib/movieHallNames';
  import './styles/Screenings.css';

  export let isLoggedIn = false;
  export let isAdmin = false;
  export let initialFilmTitle: string | null = null;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goFilmEdit: void;
    goProfile: void;
    goCart: void;
  }>();

  type ScreeningDetails = Screening & {
    filmTitle: string;
  };
  type MovieScheduleGroup = {
    filmTitle: string;
    screenings: ScreeningDetails[];
  };
  type ScheduleDaySection = {
    date: string;
    groups: MovieScheduleGroup[];
  };

  function groupScreeningsByFilm(dayScreenings: ScreeningDetails[]): MovieScheduleGroup[] {
    const map = new Map<string, ScreeningDetails[]>();
    for (const screening of dayScreenings) {
      const key = screening.filmTitle;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(screening);
    }
    return [...map.entries()].map(([filmTitle, list]) => ({ filmTitle, screenings: list }));
  }

  let screenings: ScreeningDetails[] = [];
  let selectedScreening: ScreeningDetails | null = null;
  let isLoading = true;
  let isDetailLoading = false;
  let error = '';
  let purchaseError = '';
  let purchaseInfo = '';

  let seatNumber = 1;
  let ticketQuantity = 1;
  let guestName = '';
  let guestEmail = '';
  let guestPhone = '';

  $: ticketQtyPreview =
    Math.min(99, Math.max(1, Math.floor(Number(ticketQuantity)))) || 1;

  $: scheduleDaySections = (() => {
    const sorted = [...screenings].sort(
      (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime()
    );
    const byDate = new Map<string, ScreeningDetails[]>();
    for (const s of sorted) {
      const day = s.startTime.slice(0, 10);
      if (!day) continue;
      const list = byDate.get(day) ?? [];
      list.push(s);
      byDate.set(day, list);
    }
    const dates = [...byDate.keys()].sort(
      (a, b) => new Date(a).getTime() - new Date(b).getTime()
    );
    return dates.map((date): ScheduleDaySection => {
      const list = byDate.get(date)!;
      return { date, groups: groupScreeningsByFilm(list) };
    });
  })();

  let scheduleFilmFilter: string | null = null;

  $: visibleDaySections = scheduleDaySections
    .map(({ date, groups }) => ({
      date,
      groups: scheduleFilmFilter ? groups.filter((g) => g.filmTitle === scheduleFilmFilter) : groups
    }))
    .filter((s) => s.groups.length > 0);

  function formatDateTime(date: string): string {
    const parsed = new Date(date);
    if (Number.isNaN(parsed.getTime())) return date;
    return parsed.toLocaleString('hu-HU');
  }

  function formatTime(date: string): string {
    const parsed = new Date(date);
    if (Number.isNaN(parsed.getTime())) return date;
    return parsed.toLocaleTimeString('hu-HU', { hour: '2-digit', minute: '2-digit' });
  }

  function formatScheduleDayHeading(dateValue: string): string {
    const parsed = new Date(`${dateValue}T12:00:00`);
    if (Number.isNaN(parsed.getTime())) return dateValue;
    return parsed.toLocaleDateString('hu-HU', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  async function loadScreenings() {
    isLoading = true;
    error = '';

    try {
      const screeningList = await getScreenings();
      screenings = await Promise.all(
        screeningList.map(async (screening) => {
          try {
            const film = await getFilm(screening.filmId);
            return { ...screening, filmTitle: film.title };
          } catch {
            return { ...screening, filmTitle: 'Ismeretlen film' };
          }
        })
      );

      scheduleFilmFilter = initialFilmTitle;
      const forFilm =
        initialFilmTitle && screenings.length > 0
          ? screenings
              .filter((s) => s.filmTitle === initialFilmTitle)
              .sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime())
          : [];
      const pickDetail =
        forFilm.length > 0 ? forFilm[0] : initialFilmTitle ? null : screenings[0] ?? null;
      selectedScreening = pickDetail;
      if (selectedScreening) {
        await loadScreeningDetails(selectedScreening.id, false);
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'Ismeretlen hiba történt.';
    } finally {
      isLoading = false;
    }
  }

  async function loadScreeningDetails(id: string, showLoader = true) {
    error = '';
    if (showLoader) {
      isDetailLoading = true;
    }

    try {
      const screening = await getScreening(id);
      const film = await getFilm(screening.filmId);
      selectedScreening = { ...screening, filmTitle: film.title };
      ticketQuantity = 1;
      purchaseError = '';
      purchaseInfo = '';
    } catch (e) {
      error = e instanceof Error ? e.message : 'A vetítés adatai nem tölthetők be.';
    } finally {
      isDetailLoading = false;
    }
  }

  function chooseScreeningTime(screening: ScreeningDetails) {
    loadScreeningDetails(screening.id);
  }

  function handleAddToCart() {
    if (!selectedScreening) return;
    purchaseError = '';
    purchaseInfo = '';

    const qty = ticketQuantity;

    if (!Number.isInteger(seatNumber) || seatNumber < 1) {
      purchaseError = 'Az ülés számának 1 vagy annál nagyobb egész számnak kell lennie.';
      return;
    }
    if (!Number.isInteger(qty) || qty < 1 || qty > 99) {
      purchaseError = 'A darabszámnak 1 és 99 közötti egész számnak kell lennie.';
      return;
    }
    if (!isLoggedIn && (!guestEmail.trim() || !guestPhone.trim())) {
      purchaseError = 'Vendég vásárlásnál az e-mail és telefonszám megadása kötelező.';
      return;
    }

    try {
      for (let i = 0; i < qty; i++) {
        cartStore.add({
          screeningId: selectedScreening.id,
          filmTitle: selectedScreening.filmTitle,
          screeningStartTime: selectedScreening.startTime,
          movieHallId: selectedScreening.movieHallId,
          seatNumber: seatNumber + i,
          ticketPrice: selectedScreening.basePrice,
          guestName: isLoggedIn ? null : guestName.trim() || null,
          guestEmail: isLoggedIn ? null : guestEmail.trim(),
          guestPhone: isLoggedIn ? null : guestPhone.trim()
        });
      }
      purchaseInfo =
        qty === 1 ? 'Jegy hozzáadva a kosárhoz.' : `${qty} jegy hozzáadva a kosárhoz.`;
      dispatch('goCart');
    } catch (e) {
      purchaseError = e instanceof Error ? e.message : 'A kosárba helyezés nem sikerült.';
    }
  }

  onMount(() => {
    loadScreenings();
  });
</script>

<div class="screenings-page">
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

  <main class="screenings-layout">
    <section class="screenings-list card">
      <h2>Előadások</h2>
      {#if isLoading}
        <p class="muted">Vetítések betöltése...</p>
      {:else if screenings.length === 0}
        <p class="muted">Jelenleg nincs aktív vetítés.</p>
      {:else}
        {#if scheduleFilmFilter}
          <div class="schedule-filter-title-bar">
            <strong>{scheduleFilmFilter}</strong>
          </div>
        {/if}

        {#if scheduleDaySections.length === 0}
          <p class="muted">Nincs kelthető vetítés a listában.</p>
        {:else if visibleDaySections.length === 0}
          <p class="muted">Ehhez a filmhez nincs vetítés a műsoron.</p>
        {:else}
          {#each visibleDaySections as section (section.date)}
            <div class="schedule-day-block">
              <h3 class="schedule-day-heading">{formatScheduleDayHeading(section.date)}</h3>
              <ul class="schedule-list">
                {#each section.groups as group}
                  <li class="schedule-item">
                    <div class="poster-placeholder">{group.filmTitle.slice(0, 1).toUpperCase()}</div>
                    <div class="schedule-content">
                      <h3>{group.filmTitle}</h3>
                      <div class="time-chips">
                        {#each group.screenings as screening}
                          <button
                            type="button"
                            class:selected={selectedScreening?.id === screening.id}
                            on:click={() => chooseScreeningTime(screening)}
                          >
                            <strong>{formatTime(screening.startTime)}</strong>
                            <span>{screening.basePrice} Ft</span>
                          </button>
                        {/each}
                      </div>
                    </div>
                  </li>
                {/each}
              </ul>
            </div>
          {/each}
        {/if}

        {#if scheduleFilmFilter && scheduleDaySections.length > 0}
          <div class="schedule-filter-footer">
            <button type="button" class="schedule-clear-filter" on:click={() => (scheduleFilmFilter = null)}>
              Teljes műsor mutatása
            </button>
          </div>
        {/if}
      {/if}
    </section>

    <section class="screening-details card">
      <h2>Jegyfoglalás</h2>
      {#if isDetailLoading}
        <p class="muted">Részletek betöltése...</p>
      {:else if selectedScreening}
        <h3>{selectedScreening.filmTitle}</h3>
        <div class="showtime-highlight">
          <strong>{formatTime(selectedScreening.startTime)}</strong>
          <span>{formatDateTime(selectedScreening.startTime)}</span>
        </div>
        <div class="details-grid">
          <p><strong>Alapár:</strong> {selectedScreening.basePrice} Ft</p>
          <p><strong>Terem:</strong> {getMovieHallDisplayName(selectedScreening.movieHallId)}</p>
        </div>
        <form class="purchase-form" on:submit|preventDefault={handleAddToCart}>
          <h4>Jegyfoglalás</h4>
          <label>
            Ülésszám
            <input type="number" min="1" step="1" bind:value={seatNumber} required />
          </label>
          <label>
            Darab
            <input type="number" min="1" max="99" step="1" bind:value={ticketQuantity} required />
          </label>
          {#if ticketQtyPreview >= 2}
            <p class="screenings-multi-seat-hint muted">
              Az ülésszám az <strong>első szék</strong>; a jegyek sorban következő ülésekre szólnak (<span
                class="screenings-seat-range"
                ><strong>{seatNumber}</strong>–<strong>{seatNumber + ticketQtyPreview - 1}</strong></span
              >).
            </p>
          {/if}

          {#if !isLoggedIn}
            <p class="screenings-guest-hint muted">
              Kérjük, add meg adataidat a fizetéshez és a jegyek kézbesítéséhez. Vendégként a vásárlásaid nem jelennek meg a profilodban – ha egy helyen kezelnéd a jegyeidet, jelentkezz be vagy regisztrálj!
            </p>
            <label>
              Név
              <input type="text" bind:value={guestName} />
            </label>
            <label>
              E-mail
              <input type="email" bind:value={guestEmail} required />
            </label>
            <label>
              Telefonszám
              <input type="tel" bind:value={guestPhone} required />
            </label>
          {/if}

          <button type="submit">
            Kosárba teszem
          </button>
        </form>
      {:else}
        <p class="muted">Válassz ki egy vetítést a listából.</p>
      {/if}
      {#if error}
        <p class="error">{error}</p>
      {/if}
      {#if purchaseError}
        <p class="error">{purchaseError}</p>
      {/if}
      {#if purchaseInfo}
        <p class="ok">{purchaseInfo}</p>
      {/if}
    </section>
  </main>
</div>
