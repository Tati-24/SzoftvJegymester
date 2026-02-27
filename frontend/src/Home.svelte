<script lang="ts">
    import { onMount } from 'svelte';
    import { getTestNames } from './lib/api';
  
    let names: string[] = [];
    let loading = true;
    let error: string | null = null;
  
    onMount(async () => {
      try {
        names = await getTestNames();
      } catch (e) {
        error = e instanceof Error ? e.message : 'Hiba a betöltéskor';
      } finally {
        loading = false;
      }
    });
  </script>
  
  <div class="page">
    <h1>Kezdőoldal</h1>
    {#if loading}
      <p>Betöltés...</p>
    {:else if error}
      <p class="error">{error}</p>
    {:else}
      <h2>Nevek a backendről:</h2>
      <ul>
        {#each names as name}
          <li>{name}</li>
        {/each}
      </ul>
    {/if}
  </div>
  
  <style>
    .page {
      max-width: 600px;
      margin: 3rem auto;
      padding: 2rem;
      color: #f5f5f5;
      font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    }
    ul {
      list-style: none;
      padding-left: 0;
    }
    li {
      padding: 0.3rem 0;
    }
    .error {
      color: #ff6b6b;
    }
  </style>