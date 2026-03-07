<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import { register } from './lib/api';
    import './Register.css';

    const dispatch = createEventDispatcher<{ goLogin: void; goHome: void; goRegister: void }>();

    let name = '';
    let email = '';
    let password = '';
    let confirmPassword = '';
    let phoneNumber = '';
  
    let loading = false;
    let error: string | null = null;
    let success = false;
  
    async function handleSubmit(event: SubmitEvent) {
      event.preventDefault();
      error = null;
      success = false;
  
      if (!name || !email || !password || !confirmPassword) {
        error = 'Minden mező kitöltése kötelező.';
        return;
      }
  
      if (password !== confirmPassword) {
        error = 'A jelszavak nem egyeznek.';
        return;
      }
  
      if (password.length < 8) {
        error = 'A jelszónak legalább 8 karakter hosszúnak kell lennie.';
        return;
      }
  
      loading = true;
      try {
        await register({
          name,
          email,
          password,
          phoneNumber: phoneNumber || null
        });

        success = true;
  
        // űrlap ürítése
        name = '';
        email = '';
        password = '';
        confirmPassword = '';
        phoneNumber = '';
      } catch (e) {
        error = e instanceof Error ? e.message : 'Ismeretlen hiba történt.';
      } finally {
        loading = false;
      }
    }
  </script>
  
  <div class="with-navbar">
    <nav class="navbar">
      <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
      <div class="navbar-menu">
        <button type="button" class="navbar-link">Vetítések</button>
        <button type="button" class="navbar-link">Filmek</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link active" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      </div>
    </nav>
    <div class="page register-page">
      <h1>Regisztráció</h1>
      {#if error}
      <p class="error">{error}</p>
    {/if}
  
    {#if success}
      <p class="success">Sikeres regisztráció (demó, nincs backend hívás)!</p>
    {/if}
  
    <form on:submit={handleSubmit}>
      <div class="field">
        <label for="name">Név</label>
        <input
          id="name"
          type="text"
          bind:value={name}
          required
        />
      </div>
  
      <div class="field">
        <label for="email">E-mail</label>
        <input
          id="email"
          type="email"
          bind:value={email}
          required
        />
      </div>

      <div class="field">
        <label for="phoneNumber">Telefonszám (nem kötelező)</label>
        <input
          id="phoneNumber"
          type="tel"
          bind:value={phoneNumber}
        />
      </div>
  
      <div class="field">
        <label for="password">Jelszó</label>
        <input
          id="password"
          type="password"
          bind:value={password}
          required
        />
      </div>
  
      <div class="field">
        <label for="confirmPassword">Jelszó megerősítése</label>
        <input
          id="confirmPassword"
          type="password"
          bind:value={confirmPassword}
          required
        />
      </div>
  
      <button type="submit" disabled={loading}>
        {#if loading}
          Küldés...
        {:else}
          Regisztráció
        {/if}
      </button>
      
      <p class="login-hint">
        Már van fiókja?
        <br>
        <button type="button" class="link-button" on:click={() => dispatch('goLogin')}>
          Bejelentkezés
        </button>
      </p>
    </form>
    </div>
  </div>