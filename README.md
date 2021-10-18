# Pokevoice
Pokevoice is a small WPF application that uses Microsoft's Cognitive Speech Service to do real-time Speech to Text for quick reference when playing Pokemon.

The app currently showcases quick information about all 898 pokemon, up to date for Generation 8 (Galar). Additional data for Alolan & Galarian forms, unique forms, e.g. Rotom (Wash) or Indeedee (Male/Female), as well as Mega Evolutions, is also available.

Additionally, Pokevoice supports changes made for popular Pokemon ROM Hacks, Renegade Platinum, and Eternal X/Wilting Y. Stat changes for those can be activated and deactivated by the vocal commands:
* Renegade Mode
* Eternal Mode
* No Mode

## Current Information Listed
* Name
* Type(s)
* Abilities
* Pre-evolutions and Post-Evolutions, as well as a short snippet of how those evolutions occur
* All 6 stats (HP, Atk, Def, SpA, SpD, Spe), including colorized bars for visualization and a cumulative stat total

## Visual Example
![Gardevoir by Pokevoice](/Images/GardevoirExample.png)

## Config
Pokevoice hooks in Microsoft's Cognitive Speech Service through Azure, and utilizes a non-public, custom speech model (that you could likely replicate if you want to - all I did was teach the baseline model how to recognize Pokemon names more easily).

The layout of the config.json file looks like:

```json
{
  "EndPointID": "YOUR-MODEL-ENDPOINT-ID",
  "MicrophoneName": "YOUR-MICROPHONE-NAME",
  "Region": "YOUR-REGION",
  "SubscriptionKey": "YOUR-SUBSCRIPTION-KEY"
}
```

Nothing complex - the Endpoint ID, Region, and Subscription Key should all be easily accessible on your Azure account.
> There is a difference between the default [Azure Portal](https://portal.azure.com/#home) and the [Speech Portal](https://speech.microsoft.com/portal)

In regards to the Microphone name, it's a hacky and personal workaround for a microphone issue on my end, and won't be necessary for most of you. Your default microphone should work fine.