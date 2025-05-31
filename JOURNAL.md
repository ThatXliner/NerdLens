# Journal

This is the dev log journal so this project can be submitted to Hack Club's Highway

## 2025-05-29

- Created the `Test0` and `Test1` Unity projects
  - `Test1` is based off of the Google Cardboard sample project. I couldn't get it to build so I just started from scratch (`Test0`, a standard "Universal 3D project") and decided to screw around by myself
  - https://developers.google.com/cardboard/develop/unity/quickstart
- Got `Test0` to build on Xcode
  - There were errors with provisioning licenses and I had to kind of (not realy) learn how Unity does Xcode builds (at a high level)
- Got `Test0` to render a cube in the game ivew
- Got `Test0` to work on my phone
  - Press Build in the Unity editor and then press Run in the Xcode (while my phone is connected)
- Integrated Google's cardboard SDK into `Test0`
- Got the stereoscopic display to work on my phone

## 2025-05-30

- Created this JOURNAL file
- Deleted `Test1`
- Created the git repository
- Added a sphere to `Test0`
- Got the camera to do headtracking
  - https://developers.google.com/cardboard/develop/unity/gvr-migration#adjust_the_scene_settings_to_be_supported_by_cardboard
  - So apparently I just had to add Tracked Pose Driver to the camera

## 2025-05-31

- Added networking code which uses TCP, just to test that the iproxy-based wired connection works
- How come Build works but Build and Run doesn't? And also the whole "your project has no ID" is bugging me
- Fixed bad gitignore
- Decided to go with custom TCP-based video streaming protocol (as it would work best with iproxy)
- Got a TCP feed working (just displaying text)
