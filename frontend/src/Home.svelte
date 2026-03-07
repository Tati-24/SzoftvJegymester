<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import './Home.css';

    const dispatch = createEventDispatcher<{ goLogin: void; goRegister: void; goHome: void }>();
  </script>

  <div class="home-page">
    <nav class="navbar">
      <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
      <div class="navbar-menu">
        <button type="button" class="navbar-link">Vetítések</button>
        <button type="button" class="navbar-link">Filmek</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      </div>
    </nav>
    <div class="card">
      <h2>Filmek és vetítések</h2>
      <p class="muted">A filmek és vetítések listája hamarosan megjelenik.</p>
    </div>
  </div>
