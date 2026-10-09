const express = require('express');
const { createClient } = require('@supabase/supabase-js');

const app = express();
app.use(express.json());

const supabase = createClient(process.env.DB_URL, process.env.DB_PUBLISHABLE);

app.post('/api/gacha/results', async (req, res) => {
    const payload = req.body;

    // Validate incoming structural data
    if (!payload || !payload.Items || payload.Items.length === 0 || !payload.Username) {
        return res.status(400).json({ error: "Invalid gacha payload data. 'Username' is required." });
    }

    try {
        console.log(`\n[SERVER] Processing 10-pull for user: ${payload.Username}`);

        // 1. Get or Create the Account ID for this user
        let { data: account, error: fetchError } = await supabase
            .from('accounts')
            .select('id')
            .eq('username', payload.Username)
            .single();

        if (fetchError && fetchError.code === 'PGRST116') { // PGRST116 means "no rows found"
            // Account doesn't exist, let's automatically create it
            const { data: newAccount, error: createError } = await supabase
                .from('accounts')
                .insert({ username: payload.Username })
                .select('id')
                .single();

            if (createError) throw createError;
            account = newAccount;
            console.log(`[SERVER] Created new account for ${payload.Username} with ID: ${account.id}`);
        } else if (fetchError) {
            throw fetchError;
        }

        // 2. Map items and explicitly attach the dynamic user_id
        const rowsToInsert = payload.Items.map(item => ({
            user_id: account.id, // Linked to accounts table
            session_id: payload.Session,
            rolled_at: payload.Timestamp,
            pull_number: item.PullNumber,
            item_name: item.Name,
            rarity: item.Rarity
        }));

        // 3. Bulk insert to Supabase
        const { error: dbError } = await supabase
            .from('gacha_history')
            .insert(rowsToInsert);

        if (dbError) {
            console.error('Supabase DB error:', dbError.message);
            return res.status(500).json({ error: 'Failed to log results to database.' });
        }

        return res.status(200).json({
            message: `Gacha results successfully logged for ${payload.Username}!`,
            userId: account.id
        });

    } catch (err) {
        console.error('Unexpected server error:', err);
        return res.status(500).json({ error: 'Internal server error.' });
    }
});

const PORT = process.env.PORT || 5000;
const HOST = '0.0.0.0';
app.listen(PORT, HOST, () => {
    console.log(`Gacha Backend running on http://${HOST}:${PORT}`);
});
