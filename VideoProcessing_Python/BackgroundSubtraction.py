import os
import cv2
import numpy as np
from tqdm import tqdm

def remove_background(video_path, bg_image_paths, output_path, bg_threshold, fg_threshold, scale_down_factor):
    bg_images = [cv2.imread(path, cv2.IMREAD_COLOR) for path in bg_image_paths]
    if any(bg is None for bg in bg_images):
        raise ValueError("Some background images can not be read")

    cap = cv2.VideoCapture(video_path)
    if not cap.isOpened():
        raise ValueError("Can not open video")

    fps = int(cap.get(cv2.CAP_PROP_FPS))
    width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    frame_count = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    fourcc = cv2.VideoWriter_fourcc(*'mp4v')
    out = cv2.VideoWriter(output_path, fourcc, fps, (width // scale_down_factor, height // scale_down_factor))

    sift = cv2.SIFT_create()
    
    # bg_keypoints_descriptors = [
    #     (sift.detectAndCompute(cv2.cvtColor(bg, cv2.COLOR_BGR2GRAY), None))
    #     for bg in bg_images
    # ]

    bg_keypoints_descriptors = []
    print("Processing background images for features...")
    for bg in tqdm(bg_images, desc="Background Feature Extraction"):
        bg_gray = cv2.cvtColor(bg, cv2.COLOR_BGR2GRAY)
        mask = (bg_gray > 0).astype(np.uint8) * 255 # 方法二：使用Numpy比较

        kp, des = sift.detectAndCompute(bg_gray, mask=mask)

        if des is None:
             print(f"Warning: No keypoints found for one of the background images (could be entirely black or lack features).")
             bg_keypoints_descriptors.append(([], None))
        else:
             bg_keypoints_descriptors.append((kp, des))
    
    bf = cv2.BFMatcher()

    for _ in tqdm(range(frame_count), desc="Processing Video Frames"):
        ret, frame = cap.read()
        if not ret:
            break
        frame = cv2.resize(frame, (width // scale_down_factor, height // scale_down_factor), interpolation=cv2.INTER_AREA)
        frame_gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        kp_frame, des_frame = sift.detectAndCompute(frame_gray, None)

        mask_fg_count_list = []
        mask_bg_grey_count_list = []
        mask_bg_list = []

        for (kp_bg, des_bg), bg_image in zip(bg_keypoints_descriptors, bg_images):
            matches = bf.knnMatch(des_bg, des_frame, k=2)
            good_matches = [m for m, n in matches if m.distance < 0.75 * n.distance]
            if len(good_matches) < 4:
                continue

            src_pts = np.float32([kp_bg[m.queryIdx].pt for m in good_matches]).reshape(-1, 1, 2)
            dst_pts = np.float32([kp_frame[m.trainIdx].pt for m in good_matches]).reshape(-1, 1, 2)

            H, _ = cv2.findHomography(src_pts, dst_pts, cv2.RANSAC, 5.0)
            # aligned_bg = cv2.warpPerspective(bg_image, H, (frame.shape[1], frame.shape[0]))
            aligned_bg = cv2.resize(bg_image, (frame.shape[1], frame.shape[0]))

            diff = cv2.absdiff(frame, aligned_bg)
            mask = cv2.cvtColor(diff, cv2.COLOR_BGR2GRAY)
            bg_grey = cv2.cvtColor(aligned_bg, cv2.COLOR_BGR2GRAY)

            mask_bg = (mask <= bg_threshold) & (bg_grey != 0)
            mask_bg_list.append(mask_bg)

            mask_fg_count = ((mask > fg_threshold) & (bg_grey != 0) & (frame_gray != 0))
            mask_bg_grey_count = (bg_grey == 0)
            mask_fg_count_list.append(mask_fg_count)
            mask_bg_grey_count_list.append(mask_bg_grey_count)
        
        combined_mask_bg = np.any(mask_bg_list, axis=0)
        binary_mask_bg = np.ones_like(combined_mask_bg, dtype=np.uint8) * 255
        binary_mask_bg[combined_mask_bg] = 0

        combined_mask_fg_count = np.sum(mask_fg_count_list, axis=0)
        combined_mask_bg_grey_count = np.sum(mask_bg_grey_count_list, axis=0)
        binary_mask_bg[combined_mask_bg_grey_count == len(mask_bg_list)] = 0

        combined_count = np.sum([combined_mask_fg_count, combined_mask_bg_grey_count], axis=0)

        combined_mask_fg_pass = (combined_count == len(mask_bg_list))
        combined_mask_fg_pass[combined_mask_fg_count <= 0] = 0

        binary_mask_fg = np.ones_like(combined_mask_fg_pass, dtype=np.uint8) * 255
        binary_mask_fg[combined_mask_fg_pass] = 0
        frame_gray_mask = np.zeros_like(frame_gray)
        frame_gray_mask[frame_gray == 0] = 255
        frame_gray_mask = cv2.dilate(frame_gray_mask, np.ones((7, 7), np.uint8), iterations=1)
        binary_mask_fg[frame_gray_mask == 255] = 255

        binary_mask_bg[frame_gray_mask == 255] = 0
        binary_mask_bg = cv2.erode(binary_mask_bg, np.ones((3, 3), np.uint8), iterations=1)
        # binary_mask_bg = cv2.erode(binary_mask_bg, np.ones((3, 3), np.uint8), iterations=2)
        # binary_mask_bg = cv2.dilate(binary_mask_bg, np.ones((5, 5), np.uint8), iterations=1)
        # binary_mask_fg = cv2.dilate(binary_mask_fg, np.ones((3, 3), np.uint8), iterations=1)
        binary_mask_fg = cv2.erode(binary_mask_fg, np.ones((5, 5), np.uint8), iterations=1)
        # binary_mask_bg = cv2.dilate(binary_mask_bg, kernel, iterations=4)

        # fill the holes in binary_mask_bg
        binary_mask_bg = cv2.morphologyEx(binary_mask_bg, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8))

        # output_frame = np.ones_like(frame) * 255
        output_frame = np.copy(frame)
        output_frame[binary_mask_bg < 100] //= 5
        output_frame[binary_mask_fg < 100] = frame[binary_mask_fg < 100]
        # output_frame[binary_mask_fg < 100] = [203, 192, 255]
        
        cv2.imshow("output_frame", output_frame)
        cv2.waitKey(1)
        out.write(output_frame)

    cap.release()
    out.release()
    cv2.destroyAllWindows()


def find_png_files(folder_path):
    png_files = []
    for entry in os.scandir(folder_path):
        if entry.is_file() and entry.name.lower().endswith('.png'):
            png_files.append(entry.path)
    return png_files

# remove_background(
#     "sheep_narrow_stabilized.mp4", 
#     find_png_files("sheep_narrow_background"), 
#     "sheep_narrow_bg_subtracted.mp4",
#     30, 80, 1
#     )

remove_background(
    "sheep_multi_lines_stabilized.mp4", 
    find_png_files("sheep_multi_lines_background"), 
    "sheep_multi_lines_bg_subtracted.mp4",
    30, 80, 1
    )