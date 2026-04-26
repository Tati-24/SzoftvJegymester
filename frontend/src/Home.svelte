<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import './Home.css';

  export let isLoggedIn = false;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goFilms: void;
    goFilmEdit: void;
  }>();
  </script>

  <div class="home-page">
    <nav class="navbar">
      <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
      <div class="navbar-menu">
        <button type="button" class="navbar-link">Vetítések</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goFilms')}>Filmek</button>
        {#if isLoggedIn}
          <button type="button" class="navbar-link" on:click={() => dispatch('goFilmEdit')}>Filmek módosítása</button>
        {/if}
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      </div>
    </nav>
    <div class="card">
      <h2>Filmek és vetítések</h2>
      <p class="muted">A filmek és vetítések listája hamarosan megjelenik.</p>
    </div>
  </div>
