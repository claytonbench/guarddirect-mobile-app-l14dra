I need a .net Maui app compatible with android.

Features:

Ability to authenticate by entering a phone number, posting to an api and then entering the code passed back.

I would like to track gos at all times when the app is clocked in and stop tracking when the app is clocked out.

aparent by the above, it should have a clock in / clock out page.  I want to keep and show the history. All clock in or outs should post to an API

I need a menu option to take pictures. The pictures should be stored only within the app and not on the device camera roll. These will be posted to an api

I need an activity report section. The section will allow for notes to be entered and stored in the app. These will be posted to an API.

I need a patrols page. The top 1/3 of the page should be a map showing the users current location in realtime. When this page loads, I want to query an api that includes a list of locations and each location has a list of checkpoints. When a location is selected, I want to plot the checkpoints on the map. As the user gets within 50 feet of a checkpoint, the lower list should light up and allow it to be checked off and turn green.