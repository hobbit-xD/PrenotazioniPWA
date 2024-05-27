<script>
	export let form;
	export let data;
</script>

<h1 class="text-3xl font-bold">Benvenuti su PrenotApp</h1>

{#if form?.success}
	<p class="underline italic text-green-700 font-medium mt-2">Prenotazione avvenuta con successo!</p>
{/if}

<form method="post" class="mt-2">
	<label class="block">
		<span class=" text-sm font-medium">Nome Prenotazione:</span>
		<input
			type="text"
			class="mt-1 px-3 py-2 bg-white border border-slate-300 rounded-md text-sm shadow-sm placeholder-slate-400"
			name="nomePrenotazione"
			autocomplete="off"
			required
		/>
	</label>
	<label class="block">
		<span class=" text-sm font-medium">Data Inizio:</span>
		<input
			type="datetime-local"
			class="mt-1 px-3 py-2 bg-white border border-slate-300 rounded-md text-sm shadow-sm placeholder-slate-400"
			name="inizioPrenotazione"
			autocomplete="off"
			required
		/>
	</label>

	<label class="block">
		<span class=" text-sm font-medium">Data Fine:</span>
		<input
			type="datetime-local"
			class="mt-1 px-3 py-2 bg-white border border-slate-300 rounded-md text-sm shadow-sm placeholder-slate-400"
			name="finePrenotazione"
			autocomplete="off"
			required
		/>
	</label>

	<button class="bg-white hover:bg-blue-500 text-gray-800 hover:text-white font-semibold py-2 px-4 mt-4 border border-gray-400 rounded shadow self-end">
		Prenota
	  </button>
</form>

<!-- <ul class="list-disc pl-5">
		{#each data.prenotazioni as prenotazione (prenotazione.id)}
			<li>{prenotazione.nomePrenotazione}</li>
		{/each}
	</ul> -->
