<script lang="ts">
  import { beforeUpdate } from 'svelte';
  import Login from './Login.svelte';
  import Home from './Home.svelte';
  import Register from './Register.svelte';
  import Films from './Films.svelte';
  import FilmEdit from './FilmEdit.svelte';
  import Screenings from './Screenings.svelte';
  import Profile from './Profile.svelte';
  import Cart from './Cart.svelte';
  import { isAdmin, isAuthenticated, setToken } from './lib/api';

  let page: 'login' | 'home' | 'register' | 'films' | 'filmEdit' | 'screenings' | 'profile' | 'cart' = 'home';
  let loggedIn = isAuthenticated();
  let admin = isAdmin();

  beforeUpdate(() => {
    if (!loggedIn || isAuthenticated()) return;
    loggedIn = false;
    admin = false;
  });

  let screeningsInitialFilmTitle: string | null = null;

  function handleLoggedIn() {
    loggedIn = true;
    admin = isAdmin();
    screeningsInitialFilmTitle = null;
    page = 'home';
  }

  function goToRegister() {
    screeningsInitialFilmTitle = null;
    page = 'register';
  }

  function goToLogin() {
    screeningsInitialFilmTitle = null;
    page = 'login';
  }

  function goToHome() {
    screeningsInitialFilmTitle = null;
    page = 'home';
  }

  function goToFilms() {
    screeningsInitialFilmTitle = null;
    page = 'films';
  }

  function goToScreenings(event?: CustomEvent) {
    const detail = event?.detail as { filmTitle?: string } | undefined;
    screeningsInitialFilmTitle = detail?.filmTitle ?? null;
    page = 'screenings';
  }

  function goToFilmEdit() {
    if (!loggedIn || !admin) {
      screeningsInitialFilmTitle = null;
      page = 'login';
      return;
    }
    screeningsInitialFilmTitle = null;
    page = 'filmEdit';
  }

  function goToProfile() {
    if (!loggedIn) {
      screeningsInitialFilmTitle = null;
      page = 'login';
      return;
    }
    screeningsInitialFilmTitle = null;
    page = 'profile';
  }

  function goToCart() {
    screeningsInitialFilmTitle = null;
    page = 'cart';
  }

  function handleLogout() {
    setToken(null);
    loggedIn = false;
    admin = false;
    screeningsInitialFilmTitle = null;
    page = 'home';
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
  />
{/if}
