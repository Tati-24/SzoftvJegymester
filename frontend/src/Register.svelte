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
  
    // Csak telefonszám formátum: számok, +, szóköz, kötőjel, zárójelek (opcionális mező)
    const PHONE_REGEX = /^[+]?[0-9\s\-()]{6,25}$/;

    async function handleSubmit(event: SubmitEvent) {
      event.preventDefault();
      error = null;
      success = false;

      if (!name || !email || !password || !confirmPassword) {
        error = 'Minden mező kitöltése kötelező (név, e-mail, jelszó, jelszó megerősítése).';
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

      if (phoneNumber.trim() !== '' && !PHONE_REGEX.test(phoneNumber.trim())) {
        error = 'A telefonszám csak számokból, +, szóközből, kötőjelből és zárójelekből állhat (pl. +36 20 123 4567).';
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
      <p class="success">Sikeres regisztráció!</p>
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
          placeholder="pelda@email.hu"
        />
        <span class="field-hint">Csak valós e-mail formátum fogadható el.</span>
      </div>

      <div class="field">
        <label for="phoneNumber">Telefonszám (nem kötelező)</label>
        <input
          id="phoneNumber"
          type="tel"
          bind:value={phoneNumber}
          placeholder="+36 20 123 4567"
        />
        <span class="field-hint">Csak telefonszám formátum (számok, +, szóköz, kötőjel, zárójel).</span>
      </div>
  
      <div class="field">
        <label for="password">Jelszó (legalább 8 karakter)</label>
        <input
          id="password"
          type="password"
          bind:value={password}
          required
          minlength="8"
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