# GameMaster App - Quality of Life (QOL) Improvements

This document outlines planned Quality of Life enhancements for the GameMaster application, focusing strictly on the user interface, the App Hub grid, and app presentation. 

## App Hub & Grid Presentation

### 1. Advanced Sorting & Filtering
* **Search Bar:** A dedicated search bar at the top of the App Hub to instantly filter connected apps by name.
* **Sorting Options:** A dropdown to sort the app grid by:
  * *Recently Connected* (default)
  * *Alphabetical (A-Z)*
  * *Platform* (Android, Web, Desktop)
  
### 2. Enhanced App Cards
* **Click to Launch:** Tapping an app card directly in the grid should instantly launch that application (via its Android launch intent).
* **Long-Press Context Menu:** Long-pressing an app card should pull up a native context menu with quick actions, specifically:
  * **App Info:** Deep-link to the Android Settings page to manage permissions or force stop.
  * **Uninstall:** Trigger the OS uninstall prompt for that specific app.
  * **Revoke History:** Remove the app from GameMaster's App Hub registry.
* **Status Indicators:** Small visual badges (e.g., a green dot) on the app icons to indicate if the app is currently active/running vs. just existing in the historical registry.
* **Hover/Press Effects:** Subtle elevation or color changes when tapping or hovering over an app card to make the grid feel more tactile and responsive.

### 3. Beautiful Empty States
* **Zero Apps Connected:** If the App Hub is empty, display a beautiful, friendly illustration with brief instructions on how to connect client apps, rather than a blank screen.
* **No Search Results:** A dedicated graphic and message when a search yields no matching apps.

### 4. Layout & Responsiveness
* **Dynamic Grid Sizing:** Ensure the grid dynamically adjusts the number of columns based on the screen width or orientation (e.g., 2 columns on portrait phones, 4-5 columns on tablets/desktop).
* **Pull-to-Refresh:** In addition to the manual "Refresh" button, implement a native swipe-down "Pull-to-Refresh" gesture on mobile devices to reload the app list.

### 5. App Details View
* **Usage Statistics:** When clicking into an app, display simple, readable statistics (e.g., "First Connected: [Date]", "Total Transactions: 15").
* **Smooth Transitions:** Implement seamless shared-element transitions (hero animations) when an app icon in the grid is tapped, seamlessly expanding into the details view.
* **Permission Management Shortcut:** Since GameMaster cannot directly toggle another app's OS-level permissions for security reasons, provide an "Open Android Settings" button on the app's detail page. This button will deep-link the user directly to that specific app's OS Settings page so they can quickly toggle permissions on/off.

### 6. General Accessibility & Theming
* **Dark Mode Support:** Ensure the App Hub grid, text, and empty states perfectly adapt to the system's light/dark mode settings.
* **Readable Typography:** Utilize scalable typography for app names in the grid, ensuring long app names truncate gracefully (e.g., `GameMaster...`) rather than breaking the layout.

## Future Feature Roadmap

### 1. The 5 Pillars of EXP (Holistic Leveling)
GameMaster will transition from a single generic "ExpPoints" pool to a 5-pillar skill tree based on the core areas of holistic wellness:
* **Physical EXP** (Exercise, diet, sleep)
* **Mental EXP** (Learning, reading, skill acquisition)
* **Emotional EXP** (Stress management, therapy, resilience)
* **Social EXP** (Relationships, community, gatherings)
* **Spiritual EXP** (Meditation, purpose, core values)

**Leveling Formula (Individual Pillars):**
Each of the 5 pillars will have its own individual level, calculated using a quadratic progression curve to make early levels fast and later levels challenging.
* `Pillar Level = floor( sqrt(Pillar_EXP / 100) ) + 1`
* *Example:* 0 EXP = Lvl 1 | 100 EXP = Lvl 2 | 400 EXP = Lvl 3 | 900 EXP = Lvl 4

**Central "Life Level" (Global Level):**
The overarching "Life Level" represents your total combined growth across all areas. It is calculated dynamically based on the sum of all 5 EXP pools, using a slightly steeper curve to encourage well-rounded growth:
* `Life Level = floor( sqrt(Total_Combined_EXP / 250) ) + 1`

**Permission Simplification:**
There will NOT be separate Android permissions for each of the 5 pillars. A single `READ_EXPPOINTS` permission will grant an app access to read all 5 pillar totals and the central Life Level.

### 2. User Profile Tab (Replacing REPL)
* **Standalone REPL:** The current REPL terminal will be extracted from GameMaster and turned into its own standalone client app (similar to CoinApp).
* **New User Tab:** The tab previously occupied by the REPL will be replaced by a dedicated **User Profile** page.
* **Profile Fields:** Users can set their Display Name, Profile Picture (Avatar), and a short local Bio.
* **Profile Permissions:** 
  * A new `READ_PROFILE` Android permission will be created so apps can read the user's name/avatar to display in their own UIs.
  * *Write* access to the profile is strictly locked to the GameMaster app itself. Third-party apps can only ever read this data, never change it.
