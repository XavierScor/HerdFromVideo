import cv2
import numpy as np
from tqdm import tqdm

def getFrameCountByLoop(video_path):
    cap = cv2.VideoCapture(video_path)
    frame_count = 0
    while(True):
        video_not_finish, _ = cap.read()
        if not video_not_finish:
            break
        frame_count += 1
    return frame_count

def stabilize_video(video_path, output_path, scale_down_factor):
    print("Press 'p' to pause, 'a' to select ROI, and 'q' to quit.")

    # Read video
    cap = cv2.VideoCapture(video_path)
    fourcc = cv2.VideoWriter_fourcc(*'mp4v')
    out = None

    # Initialize variables
    warp_stack = []
    imgs = []
    H_tot = np.eye(3)
    trackers = []
    regions = []
    pause = True
    selecting = False

    # Read the first frame
    ret, prev_img = cap.read()
    if not ret:
        raise RuntimeError("Failed to read video")
    prev_img = cv2.resize(prev_img, (prev_img.shape[1] // scale_down_factor, prev_img.shape[0] // scale_down_factor))
    imgs.append(prev_img)

    frame_count = int(getFrameCountByLoop(video_path))

    for _ in tqdm(range(frame_count - 1), desc="Processing frames"):
        ret, img = cap.read()
        if not ret:
            break
        img = cv2.resize(img, (img.shape[1] // scale_down_factor, img.shape[0] // scale_down_factor))
        imgs.append(img)

        while True:
            # Show the current frame
            cv2.imshow("Frame", img)
            key = cv2.waitKey(1) & 0xFF
            if key == ord('p'):
                pause = not pause
            elif key == ord('a') and not selecting:
                selecting = True
                roi = cv2.selectROI("Frame", img, False, False)
                if roi != (0, 0, 0, 0):
                    tracker = cv2.TrackerCSRT_create()
                    tracker.init(img, roi)
                    trackers.append(tracker)
                    regions.append(roi)
                selecting = False
            elif key == ord('q'):
                break
            if not pause:
                break
        
        # Update trackers and get points
        points = []
        for tracker in trackers:
            success, box = tracker.update(img)
            if success:
                x, y, w, h = [int(v) for v in box]
                roi_gray = cv2.cvtColor(img[y:y+h, x:x+w], cv2.COLOR_BGR2GRAY)
                keypoints = cv2.goodFeaturesToTrack(roi_gray, maxCorners=100, qualityLevel=0.01, minDistance=5)
                if keypoints is not None:
                    keypoints = keypoints.reshape(-1, 2)
                    keypoints[:, 0] += x
                    keypoints[:, 1] += y
                    points.extend(keypoints.tolist())
        points = np.array(points, dtype=np.float32) if points else None

        # Calculate warping matrix
        imga = cv2.cvtColor(prev_img, cv2.COLOR_BGR2GRAY).astype(np.uint8)
        imgb = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY).astype(np.uint8)
        warp_matrix = np.eye(3, dtype=np.float32)
        if points is not None and len(points) > 4:
            prev_points = np.float32([p for p in points])
            next_points, status, _ = cv2.calcOpticalFlowPyrLK(imga, imgb, prev_points, None)
            valid = status.flatten() == 1
            if np.sum(valid) > 4:
                prev_points = prev_points[valid]
                next_points = next_points[valid]
                warp_matrix[:2, :] = cv2.estimateAffinePartial2D(prev_points, next_points)[0]
        else:
            warp_matrix[:2, :] = cv2.findTransformECC(imga, imgb, np.eye(2, 3, dtype=np.float32), cv2.MOTION_EUCLIDEAN)[1]
        
        warp_stack.append(warp_matrix)
        prev_img = img

    cap.release()
    warp_stack = np.array(warp_stack)

    # Calculate the bounding box for the warped images
    corners = np.array([[0, 0, 1], [imgs[0].shape[1], 0, 1], [0, imgs[0].shape[0], 1], [imgs[0].shape[1], imgs[0].shape[0], 1]]).T
    warp_prev = np.eye(3)
    maxmin = []
    for warp in warp_stack:
        warp = np.matmul(warp, warp_prev)
        warp_inv = np.linalg.inv(warp)
        new_corners = np.matmul(warp_inv, corners)
        maxmin += [[new_corners[1].max(), new_corners[0].max()], [new_corners[1].min(), new_corners[0].min()]]
        warp_prev = warp.copy()
    maxmin = np.array(maxmin)
    top, bottom, left, right = int(-maxmin[:, 0].min()), int(maxmin[:, 0].max() - imgs[0].shape[0]), int(-maxmin[:, 1].min()), int(maxmin[:, 1].max() - imgs[0].shape[1])

    # Create output video
    out = cv2.VideoWriter(output_path, fourcc, 30, (imgs[0].shape[1] + left + right, imgs[0].shape[0] + top + bottom))
    H_tot = np.eye(3)

    for i in tqdm(range(len(imgs)), desc="Warping frames"):
        if i == 0:
            H_inv = np.eye(3) + np.array([[0, 0, left], [0, 0, top], [0, 0, 0]])
        else:
            H_tot = np.matmul(warp_stack[i - 1], H_tot)
            H_inv = np.linalg.inv(H_tot) + np.array([[0, 0, left], [0, 0, top], [0, 0, 0]])
        img_warp = cv2.warpPerspective(imgs[i], H_inv, (imgs[i].shape[1] + left + right, imgs[i].shape[0] + top + bottom))
        out.write(img_warp)

    out.release()
    cv2.destroyAllWindows()


# stabilize_video("002_sheep_narrow/sheep_narrow.mp4", "sheep_narrow_stabilized.mp4", 2)
stabilize_video("002_sheep_multi_lines/sheep_multi_lines.mp4", "sheep_multi_lines_stabilized.mp4", 3)