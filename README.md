# HerdFromVideo
So far, this project only supports windows platform because DLL is compiled on windows.

MacOS support will be updated later.

## Video Processing
Implemented with python and Qt (for GUI).

Please process video in following steps:
1. Stabilize the video to fix camera position with Stabilization.py
2. Save video frames with SaveVideoFrames.py
3. Use external image editing application (e.g. Gimp) to get several background images from video frames.
4. Remove background with BackgroundSubtraction.py
5. Extract video data with DataExtraction.py

## Optimization and Simulation
Implemented with unity and C#.

Graphic card is required for acceleration.

## Copyright
Videos used in this project were obtained from online sources. All rights and copyrights belong to the original creators. If you own the copyright to any video used here and object to its inclusion, please contact us at [xianjin.gong@ip-paris.fr], and we will remove it immediately.
