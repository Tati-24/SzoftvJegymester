<script lang="ts">
    import { createEventDispatcher } from 'svelte';
    import { login } from './lib/api';
  
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
  </script>
  
  <div class="page">
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
        <a href="/register">Kattintson ide a regisztrációhoz</a>
      </p>
    </form>
  </div>
  
  <style>
    .page {
      max-width: 400px;
      margin: 3rem auto;
      padding: 2rem;
      border-radius: 12px;
      background: #181818;
      color: #f5f5f5;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.4);
      font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    }
    form {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }
    label {
      display: flex;
      flex-direction: column;
      font-size: 0.9rem;
    }
    input {
      margin-top: 0.25rem;
      padding: 0.6rem 0.8rem;
      border-radius: 8px;
      border: 1px solid #333;
      background: #101010;
      color: #f5f5f5;
    }
    button {
      padding: 0.7rem 1rem;
      border-radius: 999px;
      border: none;
      background: #ff4b4b;
      color: #fff;
      font-weight: 600;
      cursor: pointer;
    }
    button:disabled {
      opacity: 0.6;
      cursor: default;
    }
    .error {
      color: #ff6b6b;
      font-size: 0.85rem;
    }
    .register-hint {
      font-size: 0.85rem;
      margin-top: 0.5rem;
      text-align: center;
    }
    .register-hint a {
      color: #646cff;
      text-decoration: none;
    }
    .register-hint a:hover {
      text-decoration: underline;
    }
  </style>