<script lang="ts">
  import Login from './Login.svelte';
  import Home from './Home.svelte';
  import Register from './Register.svelte';
  import Films from './Films.svelte';
  import FilmEdit from './FilmEdit.svelte';
  import { isAuthenticated } from './lib/api';

  let page: 'login' | 'home' | 'register' | 'films' | 'filmEdit' = 'home';
  let loggedIn = isAuthenticated();

  function handleLoggedIn() {
    loggedIn = true;
    page = 'home';
  }

  function goToRegister() {
    page = 'register';
  }

  function goToLogin() {
    page = 'login';
  }

  function goToHome() {
    page = 'home';
  }

  function goToFilms() {
    page = 'films';
  }

  function goToFilmEdit() {
    if (!loggedIn) {
      page = 'login';
      return;
    }
    page = 'filmEdit';
  }
</script>

{#if page === 'home'}
  <Home
    isLoggedIn={loggedIn}
    on:goLogin={goToLogin}
    on:goRegister={goToRegister}
    on:goHome={goToHome}
    on:goFilms={goToFilms}
    on:goFilmEdit={goToFilmEdit}
  />
{:else if page === 'login'}
  <div class="center-page">
    <Login
      on:loggedIn={handleLoggedIn}
      on:goRegister={goToRegister}
      on:goHome={goToHome}
      on:goFilms={goToFilms}
    />
  </div>
{:else if page === 'register'}
  <div class="center-page">
    <Register on:goLogin={goToLogin} on:goHome={goToHome} on:goFilms={goToFilms} />
  </div>
{:else if page === 'films'}
  <Films
    isLoggedIn={loggedIn}
    on:goLogin={goToLogin}
    on:goRegister={goToRegister}
    on:goHome={goToHome}
    on:goFilmEdit={goToFilmEdit}
  />
{:else if page === 'filmEdit'}
  <FilmEdit isLoggedIn={loggedIn} on:goLogin={goToLogin} on:goRegister={goToRegister} on:goHome={goToHome} on:goFilms={goToFilms} />
{/if}