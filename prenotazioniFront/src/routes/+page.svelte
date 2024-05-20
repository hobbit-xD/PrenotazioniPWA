<script>
	export let form;
</script>

<h1>Welcome to SvelteKit</h1>
<p>Visit <a href="https://kit.svelte.dev">kit.svelte.dev</a> to read the documentation</p>

{#if form?.success}
  <p>Added todo! 🥳</p>
{/if}

<form method="post">
	<label
		>Nome Prenotazione:
		<input name="nomePrenotazione" autocomplete="off" />
	</label>

	<label
		>Data Inizio:
		<input type="datetime-local" name="inizioPrenotazione" autocomplete="off" />
	</label>

	<label
		>Data Fine:
		<input type="datetime-local" name="finePrenotazione" autocomplete="off" />
	</label>
</form>
