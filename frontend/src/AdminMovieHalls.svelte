<script lang="ts">
  import { createEventDispatcher, onMount } from 'svelte';
  import {
    createMovieHall,
    deleteMovieHall,
    getMovieHalls,
    updateMovieHall,
    type MovieHall
  } from './lib/api';
  import NavbarBackToHome from './NavbarBackToHome.svelte';
  import ConfirmDialog from './ConfirmDialog.svelte';
  import './styles/FilmEdit.css';
  import './styles/AdminDesk.css';

  export let isLoggedIn = false;
  export let isAdmin = false;
  export let isCashier = false;

  const dispatch = createEventDispatcher<{
    goLogin: void;
    goRegister: void;
    goHome: void;
    goFilmEdit: void;
    goAdminHalls: void;
    goAdminScreenings: void;
    goAdminTickets: void;
    goCashier: void;
    goProfile: void;
    goCart: void;
    logout: void;
  }>();

  let halls: MovieHall[] = [];
  let isLoading = true;
  let saveLoading = false;
  let deleteLoading = false;
  let error = '';
  let info = '';
  let isNewMode = false;
  let showDeleteConfirm = false;

  let formId = '';
  let hallName = '';
  let seatCount = 120;
  let isOccupied = false;

  function emptyForm() {
    formId = '';
    hallName = '';
    seatCount = 120;
    isOccupied = false;
    isNewMode = true;
  }

  function mapHall(h: MovieHall) {
    formId = h.id;
    hallName = h.hallName;
    seatCount = h.seatCount;
    isOccupied = h.isOccupied;
    isNewMode = false;
  }

  async function load() {
    isLoading = true;
    error = '';
    try {
      halls = await getMovieHalls();
      if (halls.length === 0) {
        emptyForm();
      } else if (!isNewMode && formId && halls.some((h) => h.id === formId)) {
        mapHall(halls.find((h) => h.id === formId)!);
      } else if (!isNewMode && halls.length > 0) {
        mapHall(halls[0]);
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'A termek betöltése nem sikerült.';
    } finally {
      isLoading = false;
    }
  }

  function onPickerChange(e: Event) {
    const v = (e.currentTarget as HTMLSelectElement).value;
    if (v === '__new__') emptyForm();
    else {
      const h = halls.find((x) => x.id === v);
      if (h) mapHall(h);
    }
  }

  function validateForm(): string | null {
    if (!hallName.trim()) return 'A terem neve kötelező.';
    if (!Number.isInteger(seatCount) || seatCount < 1) return 'A férőhely legalább 1 legyen.';
    return null;
  }

  async function handleSave() {
    info = '';
    error = '';
    const v = validateForm();
    if (v) {
      error = v;
      return;
    }
    saveLoading = true;
    try {
      if (isNewMode) {
        const created = await createMovieHall({
          hallName: hallName.trim(),
          seatCount,
          isOccupied
        });
        info = 'Terem létrehozva.';
        isNewMode = false;
        await load();
        mapHall(created);
      } else {
        await updateMovieHall(formId, {
          hallName: hallName.trim(),
          seatCount,
          isOccupied
        });
        info = 'Változások elmentve.';
        await load();
      }
    } catch (e) {
      error = e instanceof Error ? e.message : 'A mentés nem sikerült.';
    } finally {
      saveLoading = false;
    }
  }

  function openDeleteConfirm() {
    if (isNewMode || !formId) return;
    showDeleteConfirm = true;
  }

  async function runConfirmedDelete() {
    if (isNewMode || !formId) return;
    info = '';
    error = '';
    deleteLoading = true;
    try {
      await deleteMovieHall(formId);
      info = 'Terem törölve.';
      showDeleteConfirm = false;
      await load();
      if (halls.length > 0) mapHall(halls[0]);
      else emptyForm();
    } catch (e) {
      error = e instanceof Error ? e.message : 'A törlés nem sikerült.';
      showDeleteConfirm = false;
    } finally {
      deleteLoading = false;
    }
  }

  onMount(() => {
    load();
  });
</script>

<div class="film-edit-page">
  <nav class="navbar">
    <div class="navbar-brand-group">
      <NavbarBackToHome on:goHome={() => dispatch('goHome')} />
      <button type="button" class="navbar-brand navbar-brand-link" on:click={() => dispatch('goHome')}>Jegymester</button>
    </div>
    <div class="navbar-menu admin-nav-row">
      {#if isAdmin}
        <button type="button" class="navbar-link" on:click={() => dispatch('goFilmEdit')}>Filmek</button>
        <button type="button" class="navbar-link active" on:click={() => dispatch('goAdminHalls')}>Mozitermek</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goAdminScreenings')}>Vetítések</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goAdminTickets')}>Jegyek</button>
      {/if}
      {#if isCashier || isAdmin}
        <button type="button" class="navbar-link" on:click={() => dispatch('goCashier')}>Pénztár</button>
      {/if}
      {#if isLoggedIn}
        <button type="button" class="navbar-link" on:click={() => dispatch('goProfile')}>Profil</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goCart')}>Kosár</button>
        <button type="button" class="navbar-link navbar-link-logout" on:click={() => dispatch('logout')}>Kijelentkezés</button>
      {:else}
        <button type="button" class="navbar-link" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
        <button type="button" class="navbar-link" on:click={() => dispatch('goRegister')}>Regisztráció</button>
      {/if}
    </div>
  </nav>

  <main class="admin-desk-grid">
    {#if !isLoggedIn || !isAdmin}
      <section class="film-form card" style="grid-column: 1 / -1;">
        <h2>Nincs jogosultság</h2>
        <p class="muted">A mozitermek kezelése csak adminisztrátornak érhető el.</p>
        <button type="button" class="save-btn" on:click={() => dispatch('goLogin')}>Bejelentkezés</button>
      </section>
    {:else}
      <section class="film-picker card admin-tools">
        <h2>Terem kiválasztása</h2>
        {#if isLoading}
          <p class="muted">Betöltés…</p>
        {:else}
          <select
            value={isNewMode ? '__new__' : formId}
            on:change={onPickerChange}
            disabled={saveLoading || deleteLoading}
          >
            <option value="__new__">Új terem…</option>
            {#each halls as h}
              <option value={h.id}>{h.hallName} ({h.seatCount} hely)</option>
            {/each}
          </select>
          <button type="button" class="picker-new-btn" on:click={emptyForm} disabled={saveLoading || deleteLoading}>
            Új terem űrlap
          </button>
        {/if}
      </section>

      <section class="film-form card">
        <h2>{isNewMode ? 'Új moziterem' : 'Terem adatai'}</h2>
        <form on:submit|preventDefault={handleSave}>
          <label>
            Terem neve
            <input type="text" bind:value={hallName} required disabled={saveLoading || deleteLoading} />
          </label>
          <label>
            Férőhelyek száma
            <input type="number" min="1" step="1" bind:value={seatCount} required disabled={saveLoading || deleteLoading} />
          </label>
          <label class="checkbox">
            <input type="checkbox" bind:checked={isOccupied} disabled={saveLoading || deleteLoading} />
            Foglalt / üzemben
          </label>
          <div class="form-actions">
            <button type="submit" class="save-btn" disabled={saveLoading || deleteLoading}>
              {saveLoading ? 'Mentés…' : isNewMode ? 'Terem létrehozása' : 'Mentés'}
            </button>
            <button
              type="button"
              class="delete-btn"
              on:click={openDeleteConfirm}
              disabled={saveLoading || deleteLoading || isNewMode || !formId}
            >
              {deleteLoading ? 'Törlés…' : 'Terem törlése'}
            </button>
          </div>
        </form>
        {#if info}<p class="ok">{info}</p>{/if}
        {#if error}<p class="error">{error}</p>{/if}
      </section>
    {/if}
  </main>

  <ConfirmDialog
    open={showDeleteConfirm}
    message="Biztosan törlöd ezt a termet?"
    busy={deleteLoading}
    on:confirm={runConfirmedDelete}
    on:cancel={() => (showDeleteConfirm = false)}
  />
</div>
