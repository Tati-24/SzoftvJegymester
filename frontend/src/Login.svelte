<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import { login } from './lib/api';
    import './Login.css';
  
    const dispatch = createEventDispatcher();
  
    let email = '';
    let password = '';
    let loading = false;
    let error: string | null = null;
  
    async function handleSubmit() {
      loading = true;
      error = null;
      try {
        await login(email, password);
        dispatch('loggedIn');
      } catch (e) {
        error = e instanceof Error ? e.message : 'Ismeretlen hiba';
      } finally {
        loading = false;
      }
    }

    function goRegister() {
      dispatch('goRegister');
    }
  </script>
  
  <div class="with-navbar">
    <nav class="navbar">
      <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
      <div class="navbar-menu">
        <button type="button" class="navbar-link">Vetítések</button>
        <button type="button" class="navbar-link">Filmek</button>
        <button type="button" class="navbar-link active" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      </div>
    </nav>
    <div class="page login-page">
      <h1>Bejelentkezés</h1>
      <form on:submit|preventDefault={handleSubmit}>
      <label>
        Email
        <input type="email" bind:value={email} required />
      </label>
  
      <label>
        Jelszó
        <input type="password" bind:value={password} required />
      </label>
  
      {#if error}
        <p class="error">{error}</p>
      {/if}
  
      <button type="submit" disabled={loading}>
        {#if loading}
          Bejelentkezés...
        {:else}
          Bejelentkezés
        {/if}
      </button>
      <p class="register-hint">
        Még nincs fiókja?
        <br>
        <button type="button" class="link-button" on:click={goRegister}>
          Kattintson ide a regisztrációhoz
        </button>
      </p>
    </form>
    </div>
  </div>