import cv2
import os
from tqdm import tqdm

def SaveVideoFrames(video_name):
    video_path = video_name + '.mp4'
    output_folder = video_name + '_frames'
    # Create output directory if not exists
    os.makedirs(output_folder, exist_ok=True)

    # Open video file
    cap = cv2.VideoCapture(video_path)

    if not cap.isOpened():
        print("Error: Could not open video file.")
        exit()

    # Get total frame count for progress tracking
    total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))

    frame_count = 0
    success = True

    # Process video with progress bar
    with tqdm(total=total_frames, desc="Processing video", unit="frame") as pbar:
        while success:
            success, frame = cap.read()
            if not success:
                break
            
            filename = f"{frame_count + 1:04d}.png"
            save_path = os.path.join(output_folder, filename)
            cv2.imwrite(save_path, frame)
            
            frame_count += 1
            pbar.update(1)  # Update progress bar

    # Release resources
    cap.release()
    print(f"\nDone! Saved {frame_count} frames to '{output_folder}' directory.")

# SaveVideoFrames("002_sheep_narrow/sheep_narrow_stabilized")
SaveVideoFrames("003_multi_lines/sheep_multi_lines")