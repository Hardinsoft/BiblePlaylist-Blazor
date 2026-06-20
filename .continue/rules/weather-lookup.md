---
name: Weather Lookup (Nominatim + Open-Meteo)
description: When the user asks for weather or forecast for a city and state, use Nominatim to geocode, then Open-Meteo for the forecast. Always follow the exact multi-step process using fetch_url_content.
---

When the user asks for the current weather, forecast, temperature, UV index, or conditions for a city and state (especially U.S. locations), you **MUST** follow this exact sequence:

### Step 1: Geocode the location using Nominatim
- Extract the **city** and **state** from the user's request.
- If the user did not clearly provide both, ask for clarification.
- Build this URL (replace placeholders and URL-encode the values):
https://nominatim.openstreetmap.org/search?city={CITY}&state={STATE}&format=json&limit=1
text- Use the `fetch_url_content` tool to fetch the URL.
- The response is a JSON array. Take the **first element** (`[0]`).
- Extract the `lat` and `lon` values from it.

### Step 2: Fetch weather data from Open-Meteo
- Using the `lat` and `lon` you just extracted, construct this exact URL:
https://api.open-meteo.com/v1/forecast?latitude={LAT}&longitude={LON}&daily=sunrise,sunset,uv_index_max,uv_index_clear_sky_max,temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_hours,precipitation_probability_max&current=temperature_2m,apparent_temperature,precipitation,cloud_cover,wind_speed_10m,wind_direction_10m,wind_gusts_10m&wind_speed_unit=mph&temperature_unit=fahrenheit&precipitation_unit=inch
text- Use the `fetch_url_content` tool again to fetch this URL.

### Step 3: Present the weather data in human-readable format
After getting the weather JSON, present it clearly to the user with the following structure:

**Current Conditions** (for the location):
- Temperature
- Feels like
- Conditions (cloud cover, precipitation)
- Wind (speed + direction + gusts)

**7-Day Forecast**:
Use a clean, readable format (table or well-formatted bullets). For each day include:
- Date
- High / Low temperature
- Precipitation chance + amount
- UV Index (max)
- Sunrise / Sunset times

Always mention the location at the top and note that data comes from Open-Meteo (via OpenStreetMap geocoding).

**Important Rules**:
- Never make up coordinates or weather data.
- Always use the `fetch_url_content` tool for both API calls.
- Do not skip steps or call the weather API without first getting coordinates from Nominatim.
- Be helpful and format the final output nicely for the user.