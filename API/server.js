const express = require('express');
const { createClient } = require('@supabase/supabase-js');

const app = express();
app.use(express.json());

// Securely check if variables exist to prevent silent crashes
const supabaseUrl = process.env.DB_URL;
const supabaseAnonKey = process.env.DB_PUBLISHABLE;

if (!supabaseUrl || !supabaseAnonKey) {
    console.error("❌ CRITICAL ERROR: SUPABASE_URL or SUPABASE_ANON_KEY environment variables are missing!");
    process.exit(1); // Safely tells Render exactly why it's stopping
}

const supabase = createClient(supabaseUrl, supabaseAnonKey);

// 2. Define the endpoint routing
app.post('/api/gacha/results', async (req, res) => {
    const payload = req.body;

    // Validate the incoming request format
    if (!payload || !payload.Items || payload.Items.length === 0) {
        return res.status(400).json({ error: "Invalid gacha payload data." });
    }

    try {
        console.log(`\nProcessing Gacha Session: ${payload.SessionId}...`);

        // 3. Map incoming data directly into database row shapes
        // We unpack the items list into a flat array of table rows
        const rowsToInsert = payload.Items.map(item => ({
            session_id: payload.SessionId,
            rolled_at: payload.Timestamp, // Inherit timestamp from client roll execution
            pull_number: item.PullNumber,
            item_name: item.Name,
            rarity: item.Rarity
        }));

        // 4. Perform a fast single-transaction bulk insert into Supabase
        const { data, error } = await supabase
            .from('gacha_history')
            .insert(rowsToInsert); // Pass the array directly for bulk processing

        if (error) {
            console.error('Supabase DB error:', error.message);
            return res.status(500).json({ error: 'Failed to log results to database.' });
        }

        console.log(`Successfully logged ${rowsToInsert.length} pulls to Supabase!`);

        return res.status(200).json({
            message: "Gacha results successfully logged to database!",
            session_id: payload.SessionId
        });

    } catch (err) {
        console.error('Unexpected server error:', err);
        return res.status(500).json({ error: 'Internal server error.' });
    }
});
const PORT = process.env.PORT || 5000;

const HOST = '0.0.0.0'; 

// 3. Update your app.listen call to include the HOST variable
app.listen(PORT, HOST, () => {
    console.log(`✅ Success! Gacha Backend running on http://${HOST}:${PORT}`);
});