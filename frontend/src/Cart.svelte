<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { get } from 'svelte/store';
  import {
    getScreening,
    getUserId,
    isAuthenticated,
    purchaseTicket,
    screeningHallLabel,
    TicketBuyerType
  } from './lib/api';
  import { cartStore } from './lib/cart';
  import NavbarBackToHome from './NavbarBackToHome.svelte';
  import './styles/Cart.css';

  export let isLoggedIn = false;
  export let isAdmin = false;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goFilmEdit: void;
    goScreenings: void;
    goProfile: void;
    goCart: void;
    logout: void;
  }>();

  let checkoutLoading = false;
  let pricingLoading = false;
  let error = '';
  let info = '';
  let showGuestSuccessExtra = false;
  let latestPrices: Record<string, number> = {};
  let pricingRequestSeq = 0;

  $: cartItems = $cartStore;

  $: sessionOutOfSync = isLoggedIn && !isAuthenticated();

  $: cartTotalFt = cartItems.reduce(
    (sum, item) => sum + (latestPrices[item.screeningId] ?? item.ticketPrice),
    0
  );

  $: void refreshLatestPrices(cartItems);

  async function refreshLatestPrices(items: typeof cartItems) {
    const requestId = ++pricingRequestSeq;
    const uniqueScreeningIds = Array.from(new Set(items.map((item) => item.screeningId)));
    if (uniqueScreeningIds.length === 0) {
      latestPrices = {};
      pricingLoading = false;
      return;
    }

    pricingLoading = true;
    try {
      const loaded = await Promise.all(
        uniqueScreeningIds.map(async (screeningId) => {
          const screening = await getScreening(screeningId);
          return [screeningId, screening.basePrice] as const;
        })
      );

      if (requestId === pricingRequestSeq) {
        latestPrices = Object.fromEntries(loaded);
      }
    } catch {
    } finally {
      if (requestId === pricingRequestSeq) {
        pricingLoading = false;
      }
    }
  }

  function formatDateTime(date: string): string {
    const parsed = new Date(date);
    if (Number.isNaN(parsed.getTime())) return date;
    return parsed.toLocaleString('hu-HU');
  }

  function handleRemove(itemId: string) {
    error = '';
    info = '';
    cartStore.remove(itemId);
  }

  function handleClear() {
    error = '';
    info = '';
    cartStore.clear();
  }

  async function handleCheckout() {
    error = '';
    info = '';
    showGuestSuccessExtra = false;

    if (cartItems.length === 0) {
      error = 'A kosár üres.';
      return;
    }

    if (isLoggedIn && !isAuthenticated()) {
      error =
        'Úgy látszik, bejelentkezettnek állítjuk a nézetet, de nincs érvényes belépésed (pl. lejárt a token). Lépj be újra a fenti gombbal — bejelentkezve nem kell vendég e-mail és telefon.';
      return;
    }

    checkoutLoading = true;
    try {
      const currentlyLoggedIn = isAuthenticated();
      const itemsSnapshot = get(cartStore);
      let purchasedCount = 0;

      for (const item of itemsSnapshot) {
        if (
          !currentlyLoggedIn &&
          (!item.guestEmail?.trim() ||
            !item.guestPhone?.trim() ||
            !String(item.guestName ?? '').trim())
        ) {
          throw new Error(
            'Vendég vásárlásnál minden kosártételhez kötelező a név, az e-mail és a telefonszám.'
          );
        }

        await getScreening(item.screeningId);

        const userId = getUserId();
        if (currentlyLoggedIn && !userId) {
          throw new Error('Nem sikerült felismerni a felhasználót. Jelentkezz be újra.');
        }

        await purchaseTicket({
          screeningId: item.screeningId,
          seatNumber: item.seatNumber,
          buyerType: currentlyLoggedIn
            ? TicketBuyerType.RegisteredUser
            : TicketBuyerType.Guest,
          userId: currentlyLoggedIn ? userId : null,
          guestName: currentlyLoggedIn ? null : String(item.guestName ?? '').trim(),
          guestEmail: currentlyLoggedIn ? null : item.guestEmail!.trim(),
          guestPhone: currentlyLoggedIn ? null : item.guestPhone!.trim()
        });
        purchasedCount += 1;
        cartStore.remove(item.id);
      }

      info =
        purchasedCount > 0
          ? `${purchasedCount} jegy sikeresen megvásárolva.`
          : 'Nincs feldolgozható tétel a kosárban.';
      showGuestSuccessExtra = !currentlyLoggedIn && purchasedCount > 0;
    } catch (e) {
      error = e instanceof Error ? e.message : 'A vásárlás nem sikerült.';
    } finally {
      checkoutLoading = false;
    }
  }
</script>

<div class="cart-page">
  <nav class="navbar">
    <div class="navbar-brand-group">
      <NavbarBackToHome on:goHome={() => dispatch('goHome')} />
      <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
    </div>
    <div class="navbar-menu">
      {#if isAdmin}
        <button type="button" class="navbar-link" on:click={() => dispatch('goFilmEdit')}>Admin felület</button>
      {/if}
      {#if isLoggedIn}
        <button type="button" class="navbar-link" on:click={() => dispatch('goProfile')}>Profil</button>
        <button type="button" class="navbar-link active" on:click={() => dispatch('goCart')}>Kosár</button>
        <button type="button" class="navbar-link navbar-link-logout" on:click={() => dispatch('logout')}>
          Kijelentkezés
        </button>
      {:else}
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      {/if}
    </div>
  </nav>

  <main class="cart-layout">
    <section class="card cart-items-section">
      <h1>Kosár</h1>
      <p class="cart-intro">
        Ellenőrizd a jegyeket, majd a jobb oldali összesítőnél véglegesítsd a vásárlást.
      </p>
      {#if sessionOutOfSync}
        <aside class="cart-session-warning" role="alert">
          <p><strong>Újra belépés szükséges.</strong> Megjelenik a Bejelentkezve állapot, de a munkamenet már hiányzik vagy lejárt — ezért a szerver vendégnek kezelne, és vendég adatot kérne. Bejelentkezve erre nincs szükség.</p>
          <button type="button" class="cart-session-warning-btn" on:click={() => dispatch('goLogin')}>
            Bejelentkezés újra
          </button>
        </aside>
      {/if}
      {#if !isLoggedIn && cartItems.length > 0}
        <aside class="cart-guest-banner" aria-label="Információ vendég vásárlóknak">
          <p>
            Nem vagy belépve: minden kosárhoz adott téthez <strong>érvényes e-mail</strong> és <strong>telefonszám</strong> szükséges a fizetéshez
            — ezeket a kosárban lévő jegyre már korábban megadtad a vetítésnél.
          </p>
          <p class="muted cart-guest-banner-sub">
            A megvásárolt jegyek a <strong>Profil › Saját jegyek</strong> között nem jelennek meg vendég vásárlásként. Ha itt szeretnéd látni őket, regisztrálj vagy jelentkezz be előtte.
          </p>
          <div class="cart-guest-banner-links">
            <button type="button" class="cart-guest-banner-link-btn" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
            <button type="button" class="cart-guest-banner-link-btn" on:click={() => dispatch('goRegister')}>Regisztráció</button>
          </div>
        </aside>
      {/if}

      {#if cartItems.length === 0}
        <div class="cart-empty">
          <h2>A kosár üres</h2>
          <p class="muted">Válassz vetítést és tegyél bele jegyet – itt fog megjelenni minden.</p>
          <button type="button" class="cart-btn-checkout cart-empty-cta" on:click={() => dispatch('goScreenings')}>
            Vetítések böngészése
          </button>
        </div>
      {:else}
        <ul class="cart-list">
          {#each cartItems as item (item.id)}
            <li class="cart-item">
              <div class="cart-item-main">
                <h3 class="cart-item-title">{item.filmTitle}</h3>
                <div class="cart-item-meta">
                  <span>{formatDateTime(item.screeningStartTime)}</span>
                  {#if item.movieHallName?.trim() || item.movieHallId}
                    <span>{screeningHallLabel(item)}</span>
                  {/if}
                  <span>{item.seatNumber}. szék</span>
                </div>
                {#if !isLoggedIn && (item.guestEmail || item.guestPhone)}
                  <div class="cart-item-guest">
                    {#if item.guestEmail}<strong>E-mail:</strong> {item.guestEmail}{/if}
                    {#if item.guestEmail && item.guestPhone} · {/if}
                    {#if item.guestPhone}<strong>Telefon:</strong> {item.guestPhone}{/if}
                  </div>
                {/if}
              </div>
              <div class="cart-item-right">
                <span class="cart-item-price"
                  >{latestPrices[item.screeningId] ?? item.ticketPrice} Ft</span
                >
                <button type="button" class="cart-item-remove" on:click={() => handleRemove(item.id)}>Eltávolítás</button>
              </div>
            </li>
          {/each}
        </ul>
      {/if}
    </section>

    <aside class="card summary-card">
      <h2 class="summary-heading">Összesítés</h2>

      {#if cartItems.length === 0}
        <p class="muted">Nincs vásárlható tétel.</p>
      {:else}
        <div class="summary-rows">
          <div class="summary-row">
            <span>Jegyek száma</span>
            <span class="summary-value">{cartItems.length} db</span>
          </div>
          {#if pricingLoading}
            <p class="muted" style="margin: 0;">Árak ellenőrzése…</p>
          {/if}
          <div class="summary-row summary-total">
            <span>Összesen</span>
            <span class="summary-value">{cartTotalFt} Ft</span>
          </div>
        </div>
      {/if}

      <div class="cart-actions">
        {#if cartItems.length > 0}
          <button
            type="button"
            class="cart-btn-checkout"
            on:click={handleCheckout}
            disabled={checkoutLoading || cartItems.length === 0 || sessionOutOfSync}
          >
            {#if checkoutLoading}
              Feldolgozás…
            {:else}
              Vásárlás megerősítése
            {/if}
          </button>
        {/if}
        <div class="cart-btn-row">
          <button type="button" class="cart-btn-outline" on:click={() => dispatch('goScreenings')}>Vissza a vetítésekhez</button>
          <button
            type="button"
            class="cart-btn-outline cart-btn-clear"
            on:click={handleClear}
            disabled={checkoutLoading || cartItems.length === 0}
          >
            Kosár ürítése
          </button>
        </div>
      </div>

      {#if info}
        <p class="ok">{info}</p>
        {#if showGuestSuccessExtra}
          <p class="cart-guest-after muted">
            Vendégként a jegyed rögzítve van a megadott e-mailhez és telefonszámhoz kapcsolódva, de a
            <strong>Profil › Saját jegyek</strong> listában itt nem jelenik meg. Ha szeretnéd, hogy a későbbi
            vásárlások egy helyen legyenek, regisztrálj vagy jelentkezz be előtte.
          </p>
          <div class="cart-guest-after-links">
            <button type="button" class="cart-btn-outline cart-guest-after-cta" on:click={() => dispatch('goRegister')}>
              Regisztráció
            </button>
            <button type="button" class="cart-btn-outline cart-guest-after-cta" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
          </div>
        {/if}
      {/if}
      {#if error}
        <p class="error">{error}</p>
      {/if}
    </aside>
  </main>
</div>
