export const actions = {
    default: async ({ cookies, request }) => {
        console.log("In server actions post form")
        const formData = await request.formData();
        const data = Object.fromEntries(formData);
        console.log(JSON.stringify(data));

        const response = await fetch('http://localhost:5082/api/v1/commands/', {
            method: 'POST',
            body: JSON.stringify(data),
            headers: { 'Content-Type': 'application/json' }
        });

        //console.log(response.json());
        return { success: true }

    }
};