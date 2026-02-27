<script lang="ts">
  import Login from './Login.svelte';
  import Home from './Home.svelte';

  let page: 'login' | 'home' = 'login';

  function handleLoggedIn() {
    page = 'home';
  }
</script>

{#if page === 'login'}
  <Login on:loggedIn={handleLoggedIn} />
{:else if page === 'home'}
  <Home />
{/if}