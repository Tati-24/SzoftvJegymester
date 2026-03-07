<script lang="ts">
  import Login from './Login.svelte';
  import Home from './Home.svelte';
  import Register from './Register.svelte';

  let page: 'login' | 'home' | 'register' = 'home';

  function handleLoggedIn() {
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
</script>

{#if page === 'home'}
  <Home on:goLogin={goToLogin} on:goRegister={goToRegister} on:goHome={goToHome} />
{:else if page === 'login'}
  <div class="center-page">
    <Login
      on:loggedIn={handleLoggedIn}
      on:goRegister={goToRegister}
      on:goHome={goToHome}
    />
  </div>
{:else if page === 'register'}
  <div class="center-page">
    <Register on:goLogin={goToLogin} on:goHome={goToHome} />
  </div>
{/if}