<script lang="ts">
  import { beforeUpdate, onMount } from 'svelte';
  import Login from './Login.svelte';
  import Home from './Home.svelte';
  import Register from './Register.svelte';
  import Films from './Films.svelte';
  import FilmEdit from './FilmEdit.svelte';
  import Screenings from './Screenings.svelte';
  import Profile from './Profile.svelte';
  import Cart from './Cart.svelte';
  import { isAdmin, isAuthenticated, setToken } from './lib/api';

  const SITE_TITLE = 'Jegymester';
  const TITLE_SEP = ' \u2013 ';

  type Page = 'login' | 'home' | 'register' | 'films' | 'filmEdit' | 'screenings' | 'profile' | 'cart';

  const CANONICAL_PATH: Record<Page, string> = {
    home: '/',
    login: '/bejelentkezes',
    register: '/regisztracio',
    films: '/filmek',
    filmEdit: '/film-szerkesztes',
    screenings: '/vetitesek',
    profile: '/profil',
    cart: '/kosar',
  };

  const PATH_TO_PAGE: Record<string, Page> = {
    '/': 'home',
    '/bejelentkezes': 'login',
    '/login': 'login',
    '/regisztracio': 'register',
    '/register': 'register',
    '/filmek': 'films',
    '/films': 'films',
    '/film-szerkesztes': 'filmEdit',
    '/film-edit': 'filmEdit',
    '/vetitesek': 'screenings',
    '/screenings': 'screenings',
    '/profil': 'profile',
    '/profile': 'profile',
    '/kosar': 'cart',
    '/cart': 'cart',
  };

  const SCREENING_PATHS = new Set(['/vetitesek', '/screenings']);

  function normalizePathname(path: string): string {
    const t = path.trim();
    if (t === '' || t === '/') return '/';
    return t.endsWith('/') ? t.slice(0, -1) || '/' : t;
  }

  function parseFilmQuery(): string | null {
    if (typeof window === 'undefined') return null;
    const filmRaw = new URLSearchParams(window.location.search).get('film');
    if (!filmRaw) return null;
    try {
      return decodeURIComponent(filmRaw);
    } catch {
      return filmRaw;
    }
  }

  function parseLocation(): { page: Page; film: string | null } {
    if (typeof window === 'undefined') return { page: 'home', film: null };
    const path = normalizePathname(window.location.pathname);
    const resolved = PATH_TO_PAGE[path];
    if (!resolved) return { page: 'home', film: null };
    if (!SCREENING_PATHS.has(path)) return { page: resolved, film: null };
    return { page: 'screenings', film: parseFilmQuery() };
  }

  function buildLocation(p: Page, film: string | null): string {
    const base = CANONICAL_PATH[p];
    if (p === 'screenings' && film) {
      return `${base}?film=${encodeURIComponent(film)}`;
    }
    return base;
  }

  function truncateForTitle(text: string, maxLen: number): string {
    const t = text.trim();
    if (t.length <= maxLen) return t;
    return `${t.slice(0, Math.max(0, maxLen - 1))}\u2026`;
  }

  function documentTitleFor(p: Page, film: string | null): string {
    const withSite = (part: string) => `${part}${TITLE_SEP}${SITE_TITLE}`;
    switch (p) {
      case 'home':
        return `${SITE_TITLE}${TITLE_SEP}Mozijegy`;
      case 'login':
        return withSite('Bejelentkezés');
      case 'register':
        return withSite('Regisztráció');
      case 'films':
        return withSite('Filmek');
      case 'filmEdit':
        return withSite('Filmek kezelése');
      case 'screenings':
        if (film?.trim()) {
          return withSite(`Vetítések: ${truncateForTitle(film, 46)}`);
        }
        return withSite('Vetítések');
      case 'profile':
        return withSite('Profil');
      case 'cart':
        return withSite('Kosár');
      default:
        return SITE_TITLE;
    }
  }

  function currentLocationString(): string {
    if (typeof window === 'undefined') return '/';
    return window.location.pathname + (window.location.search || '');
  }

  function pushRoute() {
    if (typeof window === 'undefined') return;
    const next = buildLocation(page, screeningsInitialFilmTitle);
    if (next !== currentLocationString()) {
      history.pushState({}, '', next);
    }
  }

  function replaceRoute() {
    if (typeof window === 'undefined') return;
    const next = buildLocation(page, screeningsInitialFilmTitle);
    if (next !== currentLocationString()) {
      history.replaceState({}, '', next);
    }
  }

  const initialRoute = parseLocation();
  let page: Page = initialRoute.page;
  let loggedIn = isAuthenticated();
  let admin = isAdmin();

  beforeUpdate(() => {
    if (!loggedIn || isAuthenticated()) return;
    loggedIn = false;
    admin = false;
  });

  let screeningsInitialFilmTitle: string | null = initialRoute.film;

  const LEGACY_TO_CANONICAL: Record<string, string> = {
    '/login': '/bejelentkezes',
    '/register': '/regisztracio',
    '/films': '/filmek',
    '/film-edit': '/film-szerkesztes',
    '/screenings': '/vetitesek',
    '/profile': '/profil',
    '/cart': '/kosar',
  };

  onMount(() => {
    const path = normalizePathname(window.location.pathname);
    const known = new Set(Object.keys(PATH_TO_PAGE));
    if (!known.has(path)) {
      page = 'home';
      screeningsInitialFilmTitle = null;
      history.replaceState({}, '', '/');
    } else {
      const canon = LEGACY_TO_CANONICAL[path];
      if (canon) {
        const next = `${canon}${window.location.search || ''}`;
        if (next !== currentLocationString()) {
          history.replaceState({}, '', next);
        }
      }
    }

    const onPop = () => {
      const r = parseLocation();
      page = r.page;
      screeningsInitialFilmTitle = r.film;
    };
    window.addEventListener('popstate', onPop);
    return () => window.removeEventListener('popstate', onPop);
  });

  $: {
    document.title = documentTitleFor(page, screeningsInitialFilmTitle);
  }

  function handleLoggedIn() {
    loggedIn = true;
    admin = isAdmin();
    screeningsInitialFilmTitle = null;
    page = 'home';
    replaceRoute();
  }

  function goToRegister() {
    screeningsInitialFilmTitle = null;
    page = 'register';
    pushRoute();
  }

  function goToLogin() {
    screeningsInitialFilmTitle = null;
    page = 'login';
    pushRoute();
  }

  function goToHome() {
    screeningsInitialFilmTitle = null;
    page = 'home';
    pushRoute();
  }

  function goToFilms() {
    screeningsInitialFilmTitle = null;
    page = 'films';
    pushRoute();
  }

  function goToScreenings(event?: CustomEvent) {
    const detail = event?.detail as { filmTitle?: string } | undefined;
    screeningsInitialFilmTitle = detail?.filmTitle ?? null;
    page = 'screenings';
    pushRoute();
  }

  function goToFilmEdit() {
    if (!loggedIn || !admin) {
      screeningsInitialFilmTitle = null;
      page = 'login';
      pushRoute();
      return;
    }
    screeningsInitialFilmTitle = null;
    page = 'filmEdit';
    pushRoute();
  }

  function goToProfile() {
    if (!loggedIn) {
      screeningsInitialFilmTitle = null;
      page = 'login';
      pushRoute();
      return;
    }
    screeningsInitialFilmTitle = null;
    page = 'profile';
    pushRoute();
  }

  function goToCart() {
    screeningsInitialFilmTitle = null;
    page = 'cart';
    pushRoute();
  }

  function handleLogout() {
    setToken(null);
    loggedIn = false;
    admin = false;
    screeningsInitialFilmTitle = null;
    page = 'home';
    replaceRoute();
  }
</script>

{#if page === 'home'}
  <Home
    isLoggedIn={loggedIn}
    isAdmin={admin}
    on:goLogin={goToLogin}
    on:goRegister={goToRegister}
    on:goHome={goToHome}
    on:goFilms={goToFilms}
    on:goScreenings={goToScreenings}
    on:goFilmEdit={goToFilmEdit}
    on:goProfile={goToProfile}
    on:goCart={goToCart}
    on:logout={handleLogout}
  />
{:else if page === 'login'}
  <div class="center-page">
    <Login
      isLoggedIn={loggedIn}
      on:loggedIn={handleLoggedIn}
      on:goRegister={goToRegister}
      on:goHome={goToHome}
      on:goProfile={goToProfile}
      on:goCart={goToCart}
      on:logout={handleLogout}
    />
  </div>
{:else if page === 'register'}
  <div class="center-page">
    <Register
      isLoggedIn={loggedIn}
      on:loggedIn={handleLoggedIn}
      on:goLogin={goToLogin}
      on:goHome={goToHome}
      on:goProfile={goToProfile}
      on:goCart={goToCart}
      on:logout={handleLogout}
    />
  </div>
{:else if page === 'films'}
  <Films
    isLoggedIn={loggedIn}
    isAdmin={admin}
    on:goLogin={goToLogin}
    on:goRegister={goToRegister}
    on:goHome={goToHome}
    on:goScreenings={goToScreenings}
    on:goFilmEdit={goToFilmEdit}
    on:goProfile={goToProfile}
    on:goCart={goToCart}
    on:logout={handleLogout}
  />
{:else if page === 'filmEdit'}
  <FilmEdit
    isLoggedIn={loggedIn}
    isAdmin={admin}
    on:goLogin={goToLogin}
    on:goRegister={goToRegister}
    on:goHome={goToHome}
    on:goProfile={goToProfile}
    on:goCart={goToCart}
    on:logout={handleLogout}
  />
{:else if page === 'screenings'}
  <Screenings
    initialFilmTitle={screeningsInitialFilmTitle}
    isLoggedIn={loggedIn}
    isAdmin={admin}
    on:goLogin={goToLogin}
    on:goRegister={goToRegister}
    on:goHome={goToHome}
    on:goFilmEdit={goToFilmEdit}
    on:goProfile={goToProfile}
    on:goCart={goToCart}
    on:logout={handleLogout}
  />
{:else if page === 'profile'}
  <Profile
    isLoggedIn={loggedIn}
    isAdmin={admin}
    on:goLogin={goToLogin}
    on:goRegister={goToRegister}
    on:goHome={goToHome}
    on:goFilmEdit={goToFilmEdit}
    on:goProfile={goToProfile}
    on:goCart={goToCart}
    on:logout={handleLogout}
  />
{:else if page === 'cart'}
  <Cart
    isLoggedIn={loggedIn}
    isAdmin={admin}
    on:goLogin={goToLogin}
    on:goRegister={goToRegister}
    on:goHome={goToHome}
    on:goFilmEdit={goToFilmEdit}
    on:goScreenings={goToScreenings}
    on:goProfile={goToProfile}
    on:goCart={goToCart}
    on:logout={handleLogout}
  />
{/if}
