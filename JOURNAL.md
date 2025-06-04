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
- got video stream to work... i just need to fine tune it... we're 80% done with the app

## 2025-06-01

- Added line of code to keep screen always on
- Got a screen recording client prototype
- Got video stream to work. The Unity app can now view the video stream of the host computer screen. That is to say, the proof-of-concept is done.


Things to do tomorrow:
- more reliable video streaming (reconnection)
- adjustable placement + distance from the camera
- Make the 3d environment a little more lovely


See this chat: https://claude.ai/share/0881de4e-a98e-4861-8927-b1ea07453463

## 2025-06-03

- Implement very spammy (but reliable) reconnection code

TODO:

- Implement adjustable placement + distance from the camera
- Make the connection one button
- Make the reconnection less spammy (time out between each send_frame)
- Error feedback to the Tauri client
- Improve connection info on the client (Unity) side
- Look into how to make the screen record more reliable (it sometimes stops screen recording when the window is minimized?? Or even covered by other apps' windows...)


In the far future:

- Branding + Logo + Packaging
