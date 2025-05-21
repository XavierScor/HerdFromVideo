import cv2 as cv
import numpy as np
from datetime import datetime
import time
import math
from enum import Enum
from tqdm import tqdm

from PySide6.QtCore import QObject, QRunnable, Signal, QThreadPool, Qt, QDir
from PySide6.QtGui import QPixmap, QImage, QPalette, QColor
from PySide6.QtWidgets import (
    QApplication, 
    QLabel, 
    QMainWindow, 
    QWidget, 
    QVBoxLayout, 
    QHBoxLayout,
    QPushButton, 
    QFileDialog,
    QSlider,
    QLineEdit,
    QSizePolicy,
    QFrame,
    QProgressBar,
    QCheckBox)

def ResizeImageLargerSideTo(image, new_length):
    longer_side = max(image.shape[0], image.shape[1])
    scale = new_length / longer_side
    return cv.resize(image, (int(math.floor(image.shape[1] * scale)), int(math.floor(image.shape[0] * scale))))

def getFrameCountByLoop(video_path):
    cap = cv.VideoCapture(video_path)
    rough_guess = int(cap.get(cv.CAP_PROP_FRAME_COUNT))
    progress_bar = tqdm(range(rough_guess), desc="Scanning " + video_path, unit="frame")

    frame_count = 0
    while(True):
        video_not_finish, _ = cap.read()
        if not video_not_finish:
            break
        frame_count += 1
        progress_bar.update(1)

    progress_bar.close()
    return frame_count

class FileType(Enum):
    IMAGE = 1
    VIDEO = 2

class VideoReaderSignals(QObject):
    finished = Signal()
    frame_ready = Signal()

class VideoProcessThread(QRunnable):
    def __init__(self, extraction_processor, main_window):
        super().__init__()
        self.main_window = main_window
        self.extraction_processor = extraction_processor
        self.signals = VideoReaderSignals()

    def run(self):
        while self.extraction_processor.extractNextFrame() and self.main_window.is_extracting:
            self.signals.frame_ready.emit()
        if self.main_window.is_extracting:
            self.signals.finished.emit()

class ExtractionProcessor():
    def __init__(self, source_file_path, gui_window):
        # NOTE: Set up source file path and determine file type
        self.source_file_path = source_file_path
        self.source_file_folder = source_file_path[:source_file_path.rindex("/")]
        if source_file_path[-3:] == 'png' or source_file_path[-3:] == 'jpg':
            self.file_type = FileType.IMAGE
        else:
            self.file_type = FileType.VIDEO

        # NOTE: Link the gui window to the extraction processor
        self.gui_window = gui_window

        self.initializeParametersByDefault()
        self.initializeFileReader()
        self.initializePreview()

    # === Initialize default parameters used in extraction ===
    def initializeParametersByDefault(self):
        self.obstacle_color = (0, 100, 140)
        self.masked_color = (0, 0, 255)

        # NOTE: Following parameters are only used for video input
        if self.file_type == FileType.VIDEO:
            self.processed_frame_count = 0
            self.overlay_arrow_multiplier = 1
            self.smooth_window_size = 9
            self.window_center_index = self.smooth_window_size // 2

            self.is_constant_total_body_pixel_count = False
            self.is_extraction_initialized = False

            self.density_video_writer = None
            self.field_video_writer = None
            self.flow_video_writer = None
            self.overlay_video_writer = None

            self.video_capture_origin = None
            self.video_capture_stabilized = None
            self.video_capture_bg_subtracted = None

    # === Test whether source file can be accessed by the path ===
    def initializeFileReader(self):
        if self.file_type == FileType.IMAGE:
            self.image = cv.imread(self.source_file_path)
            if self.image is None:
                self.gui_window.displayInformation("Image cannot be opened")
            else:
                self.gui_window.displayInformation("Image is opened successfully")
        elif self.file_type == FileType.VIDEO:
            self.video_capture_origin = cv.VideoCapture(self.source_file_path)

            self.stabilized_video_path = self.source_file_path[0:-4] + "_stabilized" + self.source_file_path[-4:]
            self.video_capture_stabilized = cv.VideoCapture(self.stabilized_video_path)

            self.bg_subtracted_video_path = self.source_file_path[0:-4] + "_bg_subtracted" + self.source_file_path[-4:]
            self.video_capture_bg_subtracted = cv.VideoCapture(self.bg_subtracted_video_path)

            if self.video_capture_origin.isOpened() and self.video_capture_stabilized.isOpened() and self.video_capture_bg_subtracted.isOpened():
                frame_count_origin = getFrameCountByLoop(self.source_file_path)
                frame_count_stabilized = getFrameCountByLoop(self.stabilized_video_path)
                frame_count_bg_subtracted = getFrameCountByLoop(self.bg_subtracted_video_path)
                if frame_count_origin == frame_count_stabilized and frame_count_origin == frame_count_bg_subtracted:
                    self.frames_per_second = self.video_capture_origin.get(cv.CAP_PROP_FPS)
                    self.frame_time = 1.0 / self.frames_per_second
                    self.total_frame_count = int(frame_count_origin)
                else:
                    print(frame_count_origin)
                    print(frame_count_stabilized)
                    print(frame_count_bg_subtracted)
                    self.gui_window.displayInformation("Videos do not match")
            else:
                self.gui_window.displayInformation("Video cannot be opened")
        else:
            self.gui_window.displayInformation("FileType error in initializeFileReader")

    def initializeFileWriter(self, resolution):
        now = datetime.now()
        self.time_stamp = now.strftime("%m_%d_%H_%M")

        with open(self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_data_meta.txt", "a") as meta_data_file:
            meta_data_file.write("Resolution " + str(resolution) + "\n")
            meta_data_file.write("GroundSize " + str(self.ground_size) + "\n")
            if self.file_type == FileType.VIDEO:
                meta_data_file.write("FrameTime " + str(self.frame_time) + "\n")
                meta_data_file.write("TotalFrameCount " + str(self.total_frame_count) + "\n")

        if self.file_type == FileType.VIDEO:
            self.density_video_writer = cv.VideoWriter(
                self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_video_density.mp4", cv.VideoWriter_fourcc(*'mp4v'), 24, (resolution * 8, resolution * 8))
            self.field_video_writer = cv.VideoWriter(
                self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_video_field.mp4", cv.VideoWriter_fourcc(*'mp4v'), 24, (resolution * 8, resolution * 8))
            self.flow_video_writer = cv.VideoWriter(
                self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_video_flow.mp4", cv.VideoWriter_fourcc(*'mp4v'), 24, self.preview_frame_bg_subtracted.shape[:2][::-1])
            self.overlay_video_writer = cv.VideoWriter(
                self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_video_overlay.mp4", cv.VideoWriter_fourcc(*'mp4v'), 24, self.preview_frame_bg_subtracted.shape[:2][::-1])

    def initializeContainers(self):
        self.frame_origin_list = []
        self.frame_stabilized_list = []
        self.frame_bg_subtracted_list = []

        self.flow_list = []
        
        self.agent_average_speed_list = [] # To calculate overall average speed

        self.speed_vs_density_bins = [] # To find relation 'speed v.s. density'
    
    def updateContainers(self):
        # NOTE: If the container has frames less than the smooth window size, fill it to meet the windows size
        #       Otherwise, pop the oldest frame and append a new one
        num_frames_to_fill = max(self.smooth_window_size - len(self.frame_stabilized_list), 0)

        for i in range(num_frames_to_fill + 1):
            video_origin_not_finished, frame_origin = self.video_capture_origin.read()
            video_stabilized_not_finished, frame_stabilized = self.video_capture_stabilized.read()
            video_bg_subtracted_not_finished, frame_bg_subtracted = self.video_capture_bg_subtracted.read()

            if video_origin_not_finished and video_stabilized_not_finished and video_bg_subtracted_not_finished:
                self.processed_frame_count += 1

                # When list is full, remove the oldest frame to empty space for the new frame
                if i >= num_frames_to_fill:
                    self.frame_origin_list.pop(0)
                    self.frame_stabilized_list.pop(0)
                    self.frame_bg_subtracted_list.pop(0)
                    self.flow_list.pop(0)

                if len(self.frame_stabilized_list) > 0:
                    prev_gray_frame = cv.cvtColor(self.frame_stabilized_list[-1], cv.COLOR_BGR2GRAY)
                else:
                    prev_gray_frame = cv.cvtColor(frame_stabilized, cv.COLOR_BGR2GRAY)
                gray_frame = cv.cvtColor(frame_stabilized, cv.COLOR_BGR2GRAY)
                flow_frame = cv.calcOpticalFlowFarneback(prev_gray_frame, gray_frame, None, 0.5, 3, 3, 3, 5, 1.1, 0)

                self.frame_origin_list.append(frame_origin)
                self.frame_stabilized_list.append(frame_stabilized)
                self.frame_bg_subtracted_list.append(frame_bg_subtracted)
                self.flow_list.append(flow_frame)
            else:
                return False
            
        return True

    # === Set preview_frame to show in the GUI ===
    def initializePreview(self):
        if self.file_type == FileType.IMAGE:
            self.preview_frame = self.image.copy()
        elif self.file_type == FileType.VIDEO:
            video_origin_not_finished, frame_origin = self.video_capture_origin.read()
            video_stabilized_not_finished, frame_stabilized = self.video_capture_stabilized.read()
            video_bg_subtracted_not_finished, frame_bg_subtracted = self.video_capture_bg_subtracted.read()
            if video_origin_not_finished and video_stabilized_not_finished and video_bg_subtracted_not_finished:
                self.preview_frame = frame_origin.copy()
                self.preview_frame_stabilized = frame_stabilized.copy()
                self.preview_frame_bg_subtracted = frame_bg_subtracted.copy()
                self.gui_window.displayInformation(
                    "Video is opened successfully \n" +
                    "There are totally " + str(self.total_frame_count) + " frames in the video")
            else:
                self.gui_window.displayInformation("Video is empty")
        else:
            self.gui_window.displayInformation("FileType error in initializePreview")

    def getPreviewFrame(self):
        return cv.cvtColor(self.preview_frame, cv.COLOR_BGR2RGB)
    
    def getPreviewFrameStabilized(self):
        if self.file_type == FileType.IMAGE:
            return cv.cvtColor(self.preview_frame, cv.COLOR_BGR2RGB)
        if self.file_type == FileType.VIDEO:
            return cv.cvtColor(self.preview_frame_stabilized, cv.COLOR_BGR2RGB)
        else:
            self.gui_window.displayInformation("FileType error in getPreviewFrameStabilized")
            return None
    
    def getPreviewFrameMasked(self, body_color, color_diff_threshold):
        if self.file_type == FileType.IMAGE:
            frame = self.preview_frame.copy()
        elif self.file_type == FileType.VIDEO:
            frame = self.preview_frame_bg_subtracted.copy()
        else:
            self.gui_window.displayInformation("FileType error in getPreviewFrameMasked")

        herdMask = self.extractHerdMask(frame, body_color, color_diff_threshold)
        frame[herdMask == 1] = self.masked_color

        return cv.cvtColor(frame, cv.COLOR_BGR2RGB)
    
    def initializeExtraction(
            self, body_color, color_diff_threshold, 
            body_pixel_coverage, body_length, 
            smooth_window_size, resolution, 
            overlay_arrow_multiplier, is_constant_total_body_pixel_count):
        
        self.body_color = body_color
        self.color_diff_threshold = color_diff_threshold
        self.body_pixel_coverage = body_pixel_coverage
        self.body_length = body_length
        self.smooth_window_size = smooth_window_size
        self.window_center_index = self.smooth_window_size // 2

        self.overlay_arrow_multiplier = overlay_arrow_multiplier
        self.is_constant_total_body_pixel_count = is_constant_total_body_pixel_count

        if self.file_type == FileType.IMAGE:
            reference_frame_for_size = self.preview_frame
            self.is_image_extracted = False
        elif self.file_type == FileType.VIDEO:
            reference_frame_for_size = self.preview_frame_bg_subtracted
        else:
            self.gui_window.displayInformation("FileType error in initializeExtraction")
            reference_frame_for_size = None
            
        ground_width = reference_frame_for_size.shape[1] / body_length
        ground_height = reference_frame_for_size.shape[0] / body_length
        self.ground_size = max(ground_width, ground_height)
        self.resolution = resolution
        self.grid_width = self.ground_size / self.resolution

        self.total_body_pixel_count = np.sum(self.extractHerdMask(reference_frame_for_size, self.body_color, self.color_diff_threshold))

        self.cell_overall_flow = np.zeros((self.resolution, self.resolution, 2))
        self.cell_has_agent_time = np.zeros((self.resolution, self.resolution))

        self.grid_accessibility = np.ones((self.resolution, self.resolution))

        picture_x = np.arange(reference_frame_for_size.shape[1])
        picture_y = np.arange(reference_frame_for_size.shape[0])
        self.picture_X, self.picture_Y = np.meshgrid(picture_x, picture_y)
        origin = np.array([reference_frame_for_size.shape[1] / 2.0, reference_frame_for_size.shape[0] / 2.0])
        self.grid_X = np.array(((self.picture_X - origin[0]) / self.body_length + self.ground_size / 2) / self.grid_width).astype(int)
        self.grid_Y = np.array(((self.picture_Y - origin[1]) / self.body_length + self.ground_size / 2) / self.grid_width).astype(int)

        self.initializeFileWriter(self.resolution)

        if self.file_type == FileType.VIDEO:
            self.initializeContainers()
            self.updateContainers()

        self.is_extraction_initialized = True

    def addObstacle(self, box):
        if self.is_extraction_initialized:
            self.grid_accessibility[self.grid_Y[box[1]:box[1]+box[3], box[0]:box[0]+box[2]], self.grid_X[box[1]:box[1]+box[3], box[0]:box[0]+box[2]]] = 0

    def extractNextFrame(self):
        if self.file_type == FileType.IMAGE:
            if not self.is_image_extracted:
                self.herd_mask = self.extractHerdMask(self.preview_frame, self.body_color, self.color_diff_threshold)
                self.pixel_num_field = np.zeros((self.resolution, self.resolution))
                agent_mask_indices = np.where(self.herd_mask > 0.5)
                for i in range(len(agent_mask_indices[0])):
                    y = agent_mask_indices[0][i]
                    x = agent_mask_indices[1][i]
                    if self.grid_X[y, x] >= 0 and self.grid_X[y, x] < self.resolution and self.grid_Y[y, x] >= 0 and self.grid_Y[y, x] < self.resolution:
                        self.pixel_num_field[self.grid_Y[y, x], self.grid_X[y, x]] += 1
                self.agent_num_field = self.pixel_num_field / self.body_pixel_coverage

                # === Write frame info to file ===
                with open(self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_data_frame.txt", "a") as frame_data_file:
                    frame_data_file.write("Frame 1 \n")
                    for y in range(self.resolution):
                        for x in range(self.resolution):
                            if self.agent_num_field[y, x] >= 0.5:
                                frame_data_file.write("Data " + str(x) + " " + str(y) + " " + str(np.rint(self.agent_num_field[y, x])) + "\n")

                self.is_image_extracted = True
                return True
            else:
                return False

        if self.updateContainers():
            self.herd_mask = self.extractHerdMask(self.frame_bg_subtracted_list[self.window_center_index], self.body_color, self.color_diff_threshold)
            current_body_pixel_count = np.sum(self.herd_mask)

            flow_mean = np.mean(self.flow_list, axis=0)

            self.pixel_num_field = np.zeros((self.resolution, self.resolution))

            if (self.grid_X[0, 0] < 0 or self.grid_X[0, 0] >= self.resolution or 
                self.grid_Y[0, 0] < 0 or self.grid_Y[0, 0] >= self.resolution or
                self.grid_X[-1, -1] < 0 or self.grid_X[-1, -1] >= self.resolution or
                self.grid_Y[-1, -1] < 0 or self.grid_Y[-1, -1] >= self.resolution or
                self.grid_X[0, -1] < 0 or self.grid_X[0, -1] >= self.resolution or
                self.grid_Y[0, -1] < 0 or self.grid_Y[0, -1] >= self.resolution or
                self.grid_X[-1, 0] < 0 or self.grid_X[-1, 0] >= self.resolution or
                self.grid_Y[-1, 0] < 0 or self.grid_Y[-1, 0] >= self.resolution):
                    
                print("Grid out of range. Please set the coordinates again.")
                self.release_writers()

                return False
            
            self.agent_velocity_x_mean = np.zeros((self.resolution, self.resolution))
            self.agent_velocity_x_var = np.zeros((self.resolution, self.resolution))
            self.agent_velocity_y_mean = np.zeros((self.resolution, self.resolution))
            self.agent_velocity_y_var = np.zeros((self.resolution, self.resolution))

            agent_mask_indices = np.where(self.herd_mask > 0.5)
            for i in range(len(agent_mask_indices[0])):
                y = agent_mask_indices[0][i]
                x = agent_mask_indices[1][i]
                if self.grid_X[y, x] >= 0 and self.grid_X[y, x] < self.resolution and self.grid_Y[y, x] >= 0 and self.grid_Y[y, x] < self.resolution:
                    self.pixel_num_field[self.grid_Y[y, x], self.grid_X[y, x]] += 1
                    self.agent_velocity_x_mean[self.grid_Y[y, x], self.grid_X[y, x]] += flow_mean[y, x, 0] / self.body_length / self.frame_time
                    self.agent_velocity_y_mean[self.grid_Y[y, x], self.grid_X[y, x]] += flow_mean[y, x, 1] / self.body_length / self.frame_time
                    self.agent_velocity_x_var[self.grid_Y[y, x], self.grid_X[y, x]] += (flow_mean[y, x, 0] / self.body_length / self.frame_time) ** 2
                    self.agent_velocity_y_var[self.grid_Y[y, x], self.grid_X[y, x]] += (flow_mean[y, x, 1] / self.body_length / self.frame_time) ** 2

            self.agent_velocity_x_mean = np.divide(self.agent_velocity_x_mean, self.pixel_num_field, out=np.zeros_like(self.agent_velocity_x_mean), where=self.pixel_num_field!=0)
            self.agent_velocity_y_mean = np.divide(self.agent_velocity_y_mean, self.pixel_num_field, out=np.zeros_like(self.agent_velocity_y_mean), where=self.pixel_num_field!=0)
            self.agent_velocity_x_var = np.divide(self.agent_velocity_x_var, self.pixel_num_field, out=np.zeros_like(self.agent_velocity_x_var), where=self.pixel_num_field!=0) - self.agent_velocity_x_mean ** 2
            self.agent_velocity_y_var = np.divide(self.agent_velocity_y_var, self.pixel_num_field, out=np.zeros_like(self.agent_velocity_y_var), where=self.pixel_num_field!=0) - self.agent_velocity_y_mean ** 2
            self.agent_velocity_x_var /= self.body_pixel_coverage
            self.agent_velocity_y_var /= self.body_pixel_coverage


            self.agent_angle_mean = np.zeros((self.resolution, self.resolution))
            self.agent_angle_var = np.zeros((self.resolution, self.resolution))
            self.cell_angle_visited_count = np.zeros((self.resolution, self.resolution))
            for i in range(len(agent_mask_indices[0])):
                y = agent_mask_indices[0][i]
                x = agent_mask_indices[1][i]
                if self.grid_X[y, x] >= 0 and self.grid_X[y, x] < self.resolution and self.grid_Y[y, x] >= 0 and self.grid_Y[y, x] < self.resolution:
                    self.cell_angle_visited_count[self.grid_Y[y, x], self.grid_X[y, x]] += 1
                    angle = 0
                    mean_velocity_magnitude = math.sqrt(
                        self.agent_velocity_x_mean[self.grid_Y[y, x], self.grid_X[y, x]] * self.agent_velocity_x_mean[self.grid_Y[y, x], self.grid_X[y, x]] + 
                        self.agent_velocity_y_mean[self.grid_Y[y, x], self.grid_X[y, x]] * self.agent_velocity_y_mean[self.grid_Y[y, x], self.grid_X[y, x]]
                    )
                    mean_flow_magnitude = math.sqrt(flow_mean[y, x, 0] * flow_mean[y, x, 0] + flow_mean[y, x, 1] * flow_mean[y, x, 1])
                    if mean_velocity_magnitude > 1e-3 and mean_flow_magnitude > 1e-3:
                        angle = math.acos(np.clip((
                            self.agent_velocity_x_mean[self.grid_Y[y, x], self.grid_X[y, x]] * flow_mean[y, x, 0] + 
                            self.agent_velocity_y_mean[self.grid_Y[y, x], self.grid_X[y, x]] * flow_mean[y, x, 1]
                        ) / (mean_velocity_magnitude * mean_flow_magnitude), -1.0, 1.0)) * (180.0 / math.pi)
                    
                    delta = angle - self.agent_angle_mean[self.grid_Y[y, x], self.grid_X[y, x]]
                    self.agent_angle_mean[self.grid_Y[y, x], self.grid_X[y, x]] += delta / (self.cell_angle_visited_count[self.grid_Y[y, x], self.grid_X[y, x]])
                    self.agent_angle_var[self.grid_Y[y, x], self.grid_X[y, x]] += delta * (angle - self.agent_angle_mean[self.grid_Y[y, x], self.grid_X[y, x]])
            self.agent_angle_var = np.divide(self.agent_angle_var, self.cell_angle_visited_count, out=np.zeros_like(self.agent_angle_var), where=self.cell_angle_visited_count!=0)
                        

            if self.is_constant_total_body_pixel_count:
                self.pixel_num_field = self.pixel_num_field * self.total_body_pixel_count / current_body_pixel_count
            self.agent_num_field = self.pixel_num_field / self.body_pixel_coverage

            agent_average_speed = np.mean(np.linalg.norm(flow_mean, axis=-1)[self.herd_mask > 0.5]) / self.body_length / self.frame_time
            self.agent_average_speed_list.append(agent_average_speed)

            # === Polarization and Angular Momentum ===
            polarization = np.zeros((2))
            angular_momentum = np.zeros((2))
            num_pixel = np.sum(np.where(self.herd_mask > 0.5, 1, 0))

            v = flow_mean[self.herd_mask > 0.5]
            polarization = np.sum(v / np.linalg.norm(v, axis=-1)[:, np.newaxis], axis=0)

            # r = np.stack((self.picture_X, self.picture_Y), axis=-1)[self.herd_mask > 0.5]
            # v_norm = np.linalg.norm(v, axis=-1)[:, np.newaxis]
            # r_norm = np.linalg.norm(r, axis=-1)[:, np.newaxis]
            # vr_norm = v_norm * r_norm
            # angular_momentum_list = np.cross(np.concatenate((r, np.zeros((r.shape[0], 1))), axis=-1), 
            #                                  np.concatenate((v, np.zeros((v.shape[0], 1))), axis=-1), axis=-1) / (vr_norm + 1e-5)
            # angular_momentum = np.sum(angular_momentum_list, axis=0)

            r_original = np.stack((self.picture_X, self.picture_Y), axis=-1)[self.herd_mask > 0.5]
            if r_original.shape[0] > 0:
                r_com = np.mean(r_original, axis=0)

                r_relative = r_original - r_com

                v_norm = np.linalg.norm(v, axis=-1)[:, np.newaxis]
                r_relative_norm = np.linalg.norm(r_relative, axis=-1)[:, np.newaxis]
                vr_norm = v_norm * r_relative_norm

                r_relative_3d = np.concatenate((r_relative, np.zeros((r_relative.shape[0], 1))), axis=-1)
                v_3d = np.concatenate((v, np.zeros((v.shape[0], 1))), axis=-1)

                angular_momentum_list = np.cross(r_relative_3d, v_3d, axis=-1)[:, 2] / (vr_norm.squeeze() + 1e-5) # 只取 z 分量并调整维度匹配

                angular_momentum = np.sum(angular_momentum_list, axis=0)

            else:
                angular_momentum = np.zeros(3) 
                angular_momentum_list = np.array([])

            if num_pixel > 0:
                polarization /= num_pixel
                angular_momentum /= num_pixel

            print("Polarization: ", np.linalg.norm(polarization))
            print("Angular Momentum: ", np.linalg.norm(angular_momentum))

            # === Overall flow in a cell ===
            for i in range(len(agent_mask_indices[0])):
                y = agent_mask_indices[0][i]
                x = agent_mask_indices[1][i]
                if self.grid_X[y, x] >= 0 and self.grid_X[y, x] < self.resolution and self.grid_Y[y, x] >= 0 and self.grid_Y[y, x] < self.resolution:
                    self.cell_overall_flow[self.grid_Y[y, x], self.grid_X[y, x]] += np.array([self.agent_velocity_x_mean[self.grid_Y[y, x], self.grid_X[y, x]], self.agent_velocity_y_mean[self.grid_Y[y, x], self.grid_X[y, x]]])
                    self.cell_has_agent_time[self.grid_Y[y, x], self.grid_X[y, x]] += 1

            # === Speed v.s. density ===
            for i in range(self.resolution):
                for j in range(self.resolution):
                    if self.agent_num_field[i, j] > 0.5:
                        if len(self.speed_vs_density_bins) < int(self.agent_num_field[i, j]) + 1:
                            for i in range(int(self.agent_num_field[i, j]) + 1 - len(self.speed_vs_density_bins)):
                                self.speed_vs_density_bins.append([])
                        self.speed_vs_density_bins[int(self.agent_num_field[i, j])].append(np.linalg.norm([self.agent_velocity_x_mean[i, j], self.agent_velocity_y_mean[i, j]]))

            # === Write frame info to file ===
            with open(self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_data_frame.txt", "a") as frame_data_file:
                frame_data_file.write("Frame " + str(self.processed_frame_count) + " AverageSpeed " + str(agent_average_speed) + 
                            " Polarization " + str(np.linalg.norm(polarization)) + " AngularMomentum " + str(np.linalg.norm(angular_momentum)) + " \n")
                for y in range(self.resolution):
                    for x in range(self.resolution):
                        if self.agent_num_field[y, x] >= 0.5:
                            frame_data_file.write("Data " + str(x) + " " + str(y) + " " + str(np.rint(self.agent_num_field[y, x])) + " " + 
                                    str(self.agent_velocity_x_mean[y, x]) + " " + str(self.agent_velocity_x_var[y, x]) + " " + 
                                    str(self.agent_velocity_y_mean[y, x]) + " " + str(self.agent_velocity_y_var[y, x]) + " " +
                                    str(self.agent_angle_mean[y, x]) + " " + str(self.agent_angle_var[y, x]) + "\n")

            return True
        else:
            self.gui_window.displayInformation("Video is finished")

            # === Write meta data ===
            with open(self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_data_meta.txt", "a") as meta_data_file:
                meta_data_file.write("OverallAverageSpeed " + str(np.mean(self.agent_average_speed_list)) + " ")
                meta_data_file.write("OverallAverageSpeedSTD " + str(np.std(self.agent_average_speed_list)) + " \n")
                meta_data_file.write("SpeedVSDensity" + "\n")
                for i in range(len(self.speed_vs_density_bins)):
                    numAgent = (int)(i + 1)
                    velAgent = 0
                    velAgentSEM = 0 # SEM because agent error calculated by pixel value
                    if len(self.speed_vs_density_bins[i]) > 0:
                        velAgent = np.mean(np.array(self.speed_vs_density_bins[i]))
                        velAgentSEM = np.std(np.array(self.speed_vs_density_bins[i])) / np.sqrt(len(self.speed_vs_density_bins[i]))
                    meta_data_file.write(str(numAgent) + " " + str(velAgent) + " " + str(velAgentSEM) + "\n")
                meta_data_file.write("SDEND" + "\n")

            # === Write accessibility data ===
            with open(self.source_file_folder + "/ExtractionOutput/" + self.time_stamp + "_data_cell.txt", "a") as cell_data_file:
                cell_data_file.write("NonAccessibleGrid " + "\n")
                for y in range(self.resolution):
                    for x in range(self.resolution):
                        if self.grid_accessibility[y, x] < 0.5:
                            cell_data_file.write("NAG " + str(x) + " " + str(y) + "\n")
                cell_data_file.write("NAGEND" + "\n")
                
                # === Write overall flow cell data ===
                for i in range(self.resolution):
                    for j in range(self.resolution):
                        if self.cell_has_agent_time[i, j] > 0:
                            self.cell_overall_flow[i, j] /= self.cell_has_agent_time[i, j]
                cell_data_file.write("GuidanceField" + "\n")
                for y in range(self.resolution):
                    for x in range(self.resolution):
                        if self.cell_has_agent_time[y, x] > 0:
                            cell_data_file.write("GF " + str(x) + " " + str(y) + " " + str(self.cell_overall_flow[y, x][0]) + " " + str(self.cell_overall_flow[y, x][1]) + "\n")
                cell_data_file.write("GFEND" + "\n")

            return False
    
    def getResolutionLimit(self, body_length):
        return int(math.floor(np.max(self.preview_frame.shape[0:2]) / body_length))
    
    def setOverlayArrowMultiplier(self, new_multiplier):
        self.overlay_arrow_multiplier = new_multiplier

    def extractHerdMask(self, frame, body_color, color_diff_threshold):
        color_diff = np.linalg.norm(np.subtract(frame, body_color), axis=2)
        mask = np.zeros(frame.shape[:2], dtype=np.uint8)
        mask[color_diff < color_diff_threshold] = 1
        return mask

    def getCurrentFrame(self):
        if self.file_type == FileType.IMAGE:
            return cv.cvtColor(self.preview_frame, cv.COLOR_BGR2RGB)
        else:
            return cv.cvtColor(self.frame_origin_list[self.window_center_index], cv.COLOR_BGR2RGB)
        
    def getCurrentFrameWithMask(self):
        if self.file_type == FileType.IMAGE:
            frame = cv.cvtColor(self.preview_frame, cv.COLOR_BGR2RGB)
            frame[self.herd_mask == 1] = [255, 0, 0]
            return frame
        else:
            frame_stabilized = cv.cvtColor(self.frame_bg_subtracted_list[self.window_center_index], cv.COLOR_BGR2RGB)
            frame_stabilized[self.herd_mask == 1] = [255, 0, 0]
            return frame_stabilized

    def getOpticalFlowRGB(self):
        if self.file_type == FileType.IMAGE:
            return cv.cvtColor(np.zeros_like(self.preview_frame), cv.COLOR_BGR2RGB)
        else:
            flow_mean = np.mean(self.flow_list, axis=0)
            frame_shape = self.frame_stabilized_list[self.window_center_index].shape
            flow_hsv_mask = np.zeros((frame_shape[0], frame_shape[1], 3), dtype=np.uint8)

            flow_hsv_mask[..., 1] = 255

            magnitude, angle = cv.cartToPolar(flow_mean[..., 0], flow_mean[..., 1], angleInDegrees=True)

            flow_hsv_mask[..., 0] = (angle / 2).astype(np.uint8)
            flow_hsv_mask[..., 2] = cv.normalize(magnitude, None, 0, 255, cv.NORM_MINMAX)

            try:
                agent_vals = self.agent_num_field[self.grid_Y, self.grid_X]
                v_channel = np.zeros_like(agent_vals, dtype=np.uint8)
                v_channel[agent_vals > 0.5] = 255
                flow_hsv_mask[..., 2] = v_channel
            except IndexError:
                print(f"Index error: Check agent_num_field ({self.agent_num_field.shape}), "
                    f"grid_Y ({self.grid_Y.shape}), and grid_X ({self.grid_X.shape})")
                raise
            except AttributeError:
                print("Attribute error: Check existance of agent_num_field, grid_Y, grid_X")
                raise

            try:
                herd_condition = self.herd_mask < 0.5
                flow_hsv_mask[herd_condition] = [179, 0, 0]
            except AttributeError:
                print("Attribute error: Check existance of herd_mask")
                raise
            except ValueError as e:
                print(f"herd_mask shape does not fit: {e}")
                print(f"flow_hsv_mask shape: {flow_hsv_mask.shape}, herd_condition shape: {herd_condition.shape}")
                raise

            rgb = cv.cvtColor(flow_hsv_mask, cv.COLOR_HSV2BGR)
            if self.flow_video_writer is not None and self.flow_video_writer.isOpened():
                self.flow_video_writer.write(rgb)
            else:
                print("Warn: no flow_video_writer")

            return rgb
    
    def getOverlayedFrame(self):
        agent_density = self.pixel_num_field / ((self.grid_width * self.body_length) * (self.grid_width * self.body_length)) * 255
        agent_density_color_map = cv.applyColorMap(agent_density.astype(np.uint8), cv.COLORMAP_BONE)
        agent_density_color_map[self.grid_accessibility < 0.5] = self.obstacle_color
        
        if self.file_type == FileType.IMAGE:
            overlay = self.preview_frame.copy()
        else:
            overlay = self.frame_stabilized_list[self.window_center_index].copy()
        agent_density_normalized = (agent_density_color_map / 255) ** 0.5
        overlay[:, :] = (overlay[:, :] * 0.4 + agent_density_normalized[self.grid_Y, self.grid_X] * 255 * 0.6).astype(np.uint8)

        if self.file_type == FileType.VIDEO:
            arrow_mask = np.zeros_like(overlay)
            grid_width_in_pixel = int(self.grid_width * self.body_length)
            for y in range(0, overlay.shape[0], grid_width_in_pixel):
                for x in range(0, overlay.shape[1], grid_width_in_pixel):
                    if self.agent_num_field[self.grid_Y[y, x], self.grid_X[y, x]] >= 0.5:
                        flow_x = self.agent_velocity_x_mean[self.grid_Y[y, x], self.grid_X[y, x]] * self.body_length * self.overlay_arrow_multiplier * self.frame_time
                        flow_y = self.agent_velocity_y_mean[self.grid_Y[y, x], self.grid_X[y, x]] * self.body_length * self.overlay_arrow_multiplier * self.frame_time
                        cv.arrowedLine(arrow_mask, (x, y), (x + int(flow_x), y + int(flow_y)), (0, 255, 255), 2)
            overlay = cv.addWeighted(overlay, 0.5, arrow_mask, 0.5, 0)

            self.overlay_video_writer.write(overlay)

        return cv.cvtColor(overlay, cv.COLOR_BGR2RGB)

    
    def getDensityField(self):
        agent_density = self.pixel_num_field / ((self.grid_width * self.body_length) * (self.grid_width * self.body_length)) * 255
        agent_density_color_map = cv.applyColorMap(agent_density.astype(np.uint8), cv.COLORMAP_BONE)
        agent_density_color_map[self.grid_accessibility < 0.5] = self.obstacle_color

        field_color_map_resized = cv.resize(agent_density_color_map, (self.resolution * 8, self.resolution * 8), interpolation=cv.INTER_NEAREST)
        if self.file_type == FileType.VIDEO:
            self.density_video_writer.write(field_color_map_resized)

            # Overlay the velocity vectors
            for i in range(self.resolution):
                for j in range(self.resolution):
                    if self.agent_num_field[i, j] >= 0.5:
                        cv.arrowedLine(field_color_map_resized, (j * 8, i * 8), 
                                    (j * 8 + int(self.agent_velocity_x_mean[i, j] * 8 * self.overlay_arrow_multiplier * self.frame_time), 
                                        i * 8 + int(self.agent_velocity_y_mean[i, j] * 8 * self.overlay_arrow_multiplier * self.frame_time)), 
                                        (0, 255, 255), 1)
                        
            self.field_video_writer.write(field_color_map_resized)
        return cv.cvtColor(field_color_map_resized, cv.COLOR_BGR2RGB)
    
    def getTotalFrameCount(self):
        if self.file_type == FileType.IMAGE:
            return 1
        else:
            return self.total_frame_count
    
    def getCurrentFrameCount(self):
        if self.file_type == FileType.IMAGE:
            return 1
        else:
            return self.processed_frame_count
    
    def release_writers(self):
        if self.file_type == FileType.VIDEO:
            print("Releasing video writers...")
            if self.density_video_writer is not None and self.density_video_writer.isOpened():
                self.density_video_writer.release()
                print("Density writer released.")
            if self.field_video_writer is not None and self.field_video_writer.isOpened():
                self.field_video_writer.release()
                print("Field writer released.")
            if self.flow_video_writer is not None and self.flow_video_writer.isOpened():
                self.flow_video_writer.release()
                print("Flow writer released.")
            if self.overlay_video_writer is not None and self.overlay_video_writer.isOpened():
                self.overlay_video_writer.release()
                print("Overlay writer released.")
    
    def __del__(self):
        if self.file_type == FileType.VIDEO:
            if self.video_capture_origin.isOpened():
                self.video_capture_origin.release()
            if self.video_capture_stabilized.isOpened():
                self.video_capture_stabilized.release()
            if self.video_capture_bg_subtracted.isOpened():
                self.video_capture_bg_subtracted.release()
        self.release_writers()


# === Widget to show color picked for pixel classification ===
class Color(QWidget):
    def __init__(self, color):
        super(Color, self).__init__()
        self.setAutoFillBackground(True)

        palette = self.palette()
        palette.setColor(QPalette.Window, QColor(color))
        self.setPalette(palette)

    def changeColor(self, color):
        palette = self.palette()
        palette.setColor(QPalette.Window, QColor(color))
        self.setPalette(palette)

class BodyPixelCoverageWindow(QWidget):
    def __init__(self):
        super().__init__()
        self.layout = QVBoxLayout()
        self.choosed_region_label = QLabel()
        self.choosed_region_label.setAlignment(Qt.AlignCenter)
        self.agent_num_enter_field = QLineEdit()
        self.agent_num_enter_field.setPlaceholderText("Enter the number of agents in the region")
        self.agent_body_length_enter_field = QLineEdit()
        self.agent_body_length_enter_field.setPlaceholderText("Enter the body length of the agents")
        self.confirm_button = QPushButton("Confirm")
        self.layout.addWidget(self.choosed_region_label)
        self.layout.addWidget(self.agent_num_enter_field)
        self.layout.addWidget(self.agent_body_length_enter_field)
        self.layout.addWidget(self.confirm_button)

        self.confirm_button.clicked.connect(self.confirm)

        self.num_pixel = 0
        self.body_pixel_coverage = 0
        self.body_length = 0

        self.setLayout(self.layout)

    def setChoosedRegion(self, frame, body_color, color_diff_threshold):
        longer_side = max(frame.shape[0], frame.shape[1])
        scale = int(800 / longer_side)
        prompt_pic = cv.resize(frame, (frame.shape[1] * scale, frame.shape[0] * scale))
        prompt_pic = cv.cvtColor(prompt_pic, cv.COLOR_BGR2RGB)
        for i in range(frame.shape[0]):
            prompt_pic = cv.line(prompt_pic, (0, i * scale), (frame.shape[1] * scale, i * scale), (0, 0, 0), 1)
        for i in range(frame.shape[1]):
            prompt_pic = cv.line(prompt_pic, (i * scale, 0), (i * scale, frame.shape[0] * scale), (0, 0, 0), 1)
        image = QImage(prompt_pic, prompt_pic.shape[1], prompt_pic.shape[0], prompt_pic.strides[0], QImage.Format_RGB888)
        self.choosed_region_label.setPixmap(QPixmap.fromImage(image))

        color_diff = np.linalg.norm(np.subtract(frame, body_color), axis=2)
        mask = np.zeros(frame.shape[:2], dtype=np.uint8)
        mask[color_diff < color_diff_threshold] = 1
        
        self.num_pixel = np.sum(mask)

    def confirm(self):
        self.body_pixel_coverage = int(self.num_pixel / int(self.agent_num_enter_field.text()))
        self.body_length = int(self.agent_body_length_enter_field.text())

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()

        self.initializeParametersByDefault()

        self.setWindowTitle("Herd Information Extraction")
        self.setMinimumSize(600, 400)

        self.initializeGUI()
        self.initializeSlots()

        self.initializeFrames()

        self.refreshDisplay()

    # === Default parameter initialization ===
    def initializeParametersByDefault(self):
        self.main_display_index = 0
        self.source_file_path = "sheep_1_bg_subtracted.mp4"
        self.resolution = 80
        self.smooth_window_size = 9
        self.is_constant_total_body_pixel_count = True
        self.body_color = [210, 240, 240]
        self.color_diff_threshold = 160
        self.body_pixel_coverage = 90
        self.body_length = 20
        self.resolution_limit = 200
        self.is_extracting = False

    # === GUI initialization ===
    def initializeGUI(self):
        # NOTE: This list contains widgets which should be disabled
        #       during extraction to avoid race condition
        self.disabled_when_extract_list = []

        # NOTE: Layout for the main center widget
        self.root_layout = QHBoxLayout()

        self.initializeDisplayPanel()
        self.initalizeControlPanel()

        self.body_pixel_coverage_window = BodyPixelCoverageWindow()

        self.root_layout.addWidget(self.display_widget)
        self.root_layout.addWidget(self.control_panel_widget)

        self.setLayout(self.root_layout)

        self.root_widget = QWidget()
        self.root_widget.setLayout(self.root_layout)
        self.setCentralWidget(self.root_widget)

    # === Left display panel initialization ===
    def initializeDisplayPanel(self):
        self.display_widget = QWidget()
        self.display_layout = QVBoxLayout()
        self.display_widget.setLayout(self.display_layout)

        self.main_display = QLabel("Main Display")
        self.main_display.setAlignment(Qt.AlignCenter)
        self.main_display.setStyleSheet("border: 2px solid black")
        self.main_display.setSizePolicy(QSizePolicy.Ignored, QSizePolicy.Ignored)

        self.choose_display_widget = QWidget()
        self.choose_display_layout = QHBoxLayout()
        self.choose_display_widget.setLayout(self.choose_display_layout)

        self.sub_displays = [QLabel("Source\nDisplay"), QLabel("Herd\nMask"), QLabel("Optical\nFlow"), QLabel("Overlayed\nDisplay"), QLabel("Density\nField")]
        self.set_button = [QPushButton("Choose") for i in range(5)]
        for i in range(5):
            sub_display_layout = QVBoxLayout()
            self.sub_displays[i].setAlignment(Qt.AlignCenter)
            self.sub_displays[i].setStyleSheet("border: 2px solid black")
            sub_display_layout.addWidget(self.sub_displays[i])
            sub_display_layout.addWidget(self.set_button[i])
            self.choose_display_layout.addLayout(sub_display_layout)

        self.display_layout.addWidget(self.main_display)
        self.display_layout.addWidget(self.choose_display_widget)
        self.display_layout.setStretchFactor(self.main_display, 4)
        self.display_layout.setStretchFactor(self.choose_display_widget, 1)

    # === Right control panel initialization ===
    def initalizeControlPanel(self):
        self.control_panel_widget = QWidget()
        self.control_panel_widget.setFixedWidth(320)
        self.control_panel_layout = QVBoxLayout()
        self.control_panel_layout.setAlignment(Qt.AlignTop)
        self.control_panel_widget.setLayout(self.control_panel_layout)

        self.initializeSourceFileWidget()
        self.initializeColorWidget()
        self.initializeBodySizeWidget()
        self.initializeExtractWidget()

        self.split_line = QFrame()
        self.split_line.setFrameShape(QFrame.HLine)
        self.control_panel_layout.addWidget(self.split_line)
        self.info_label = QLabel("Output Information")
        self.info_label.setAlignment(Qt.AlignLeft)
        self.control_panel_layout.addWidget(self.info_label)

    def initializeSourceFileWidget(self):
        self.open_file_button = QPushButton('Open File')
        self.disabled_when_extract_list.append(self.open_file_button)
        self.source_file_label = QLabel(self.source_file_path)
        layout_open_file = QHBoxLayout()
        layout_open_file.addWidget(self.open_file_button)
        layout_open_file.addWidget(self.source_file_label)
        layout_open_file.setStretchFactor(self.open_file_button, 1)
        layout_open_file.setStretchFactor(self.source_file_label, 5)
        self.control_panel_layout.addLayout(layout_open_file)

    def initializeColorWidget(self):
        # === GUI to pick body color by opencv ===
        self.pick_body_color_button = QPushButton('Pick Body Color')
        self.disabled_when_extract_list.append(self.pick_body_color_button)
        self.body_color_label = Color(QColor(self.body_color[0], self.body_color[1], self.body_color[2]))
        layout_pick_body_color = QHBoxLayout()
        layout_pick_body_color.addWidget(self.pick_body_color_button)
        layout_pick_body_color.addWidget(self.body_color_label)
        layout_pick_body_color.setStretchFactor(self.pick_body_color_button, 3)
        layout_pick_body_color.setStretchFactor(self.body_color_label, 5)
        self.control_panel_layout.addLayout(layout_pick_body_color)

        # === GUI to set body color by RGB ===
        self.body_color_r_label = QLabel("R")
        self.body_color_r_input = QLineEdit(str(self.body_color[0]))
        self.body_color_g_label = QLabel("G")
        self.body_color_g_input = QLineEdit(str(self.body_color[1]))
        self.body_color_b_label = QLabel("B")
        self.body_color_b_input = QLineEdit(str(self.body_color[2]))
        self.disabled_when_extract_list.append(self.body_color_r_input)
        self.disabled_when_extract_list.append(self.body_color_g_input)
        self.disabled_when_extract_list.append(self.body_color_b_input)
        layout_rgb_body_color = QHBoxLayout()
        layout_rgb_body_color.addWidget(self.body_color_r_label)
        layout_rgb_body_color.addWidget(self.body_color_r_input)
        layout_rgb_body_color.addWidget(self.body_color_g_label)
        layout_rgb_body_color.addWidget(self.body_color_g_input)
        layout_rgb_body_color.addWidget(self.body_color_b_label)
        layout_rgb_body_color.addWidget(self.body_color_b_input)
        self.control_panel_layout.addLayout(layout_rgb_body_color)

        # === GUI to set color difference threshold ===
        self.color_diff_threshold_label = QLabel("Color Threshold: " + str(self.color_diff_threshold))
        self.color_diff_threshold_label.setAlignment(Qt.AlignCenter)
        self.color_diff_threshold_slider = QSlider(Qt.Horizontal)
        self.color_diff_threshold_slider.setMinimum(0)
        self.color_diff_threshold_slider.setMaximum(200)
        self.color_diff_threshold_slider.setValue(self.color_diff_threshold)
        self.disabled_when_extract_list.append(self.color_diff_threshold_slider)
        layout_color_diff_threshold = QHBoxLayout()
        layout_color_diff_threshold.addWidget(self.color_diff_threshold_label)
        layout_color_diff_threshold.addWidget(self.color_diff_threshold_slider)
        layout_color_diff_threshold.setStretchFactor(self.color_diff_threshold_label, 1)
        layout_color_diff_threshold.setStretchFactor(self.color_diff_threshold_slider, 5)
        self.control_panel_layout.addLayout(layout_color_diff_threshold)

    def initializeBodySizeWidget(self):
        self.body_pixel_coverage_button = QPushButton('Find Body Pixel Coverage by OpenCV')
        self.body_area_label = QLabel("Area")
        self.body_pixel_coverage_input = QLineEdit(str(self.body_pixel_coverage))
        self.body_length_label = QLabel("Length")
        self.body_length_input = QLineEdit(str(self.body_length))
        self.disabled_when_extract_list.append(self.body_pixel_coverage_button)
        self.disabled_when_extract_list.append(self.body_pixel_coverage_input)
        self.disabled_when_extract_list.append(self.body_length_input)
        layout_body_pixel_coverage = QHBoxLayout()
        self.control_panel_layout.addWidget(self.body_pixel_coverage_button)
        layout_body_pixel_coverage.addWidget(self.body_area_label)
        layout_body_pixel_coverage.addWidget(self.body_pixel_coverage_input)
        layout_body_pixel_coverage.addWidget(self.body_length_label)
        layout_body_pixel_coverage.addWidget(self.body_length_input)
        self.control_panel_layout.addLayout(layout_body_pixel_coverage)

    def initializeExtractWidget(self):
        # === Extracted overlay arrow multiplier ===
        self.overlay_arrow_multiplier_label = QLabel("Overlay Arrow Multiplier: 4")
        self.overlay_arrow_multiplier_slider = QSlider(Qt.Horizontal)
        self.overlay_arrow_multiplier_slider.setMinimum(1)
        self.overlay_arrow_multiplier_slider.setMaximum(20)
        self.overlay_arrow_multiplier_slider.setValue(4)
        arrow_multiplier_layout = QHBoxLayout()
        arrow_multiplier_layout.addWidget(self.overlay_arrow_multiplier_label)
        arrow_multiplier_layout.addWidget(self.overlay_arrow_multiplier_slider)
        self.control_panel_layout.addLayout(arrow_multiplier_layout)

        # === Constant Total Body Pixel Count Checkbox ===
        self.constant_total_body_pixel_count_checkbox = QCheckBox("Constant Total Body Pixel Count")
        self.constant_total_body_pixel_count_checkbox.setChecked(self.is_constant_total_body_pixel_count)
        self.control_panel_layout.addWidget(self.constant_total_body_pixel_count_checkbox)

        # === Smooth Window Size and Resolution Input ===
        self.smooth_window_size_label = QLabel("Smooth Size: ")
        self.smooth_window_size_input = QLineEdit(str(self.smooth_window_size))
        self.resolution_label = QLabel("Resolution: ")
        self.resolution_input = QLineEdit(str(self.resolution))
        self.resolution_limit_label = QLabel("< " + str(int(self.resolution_limit)))
        self.disabled_when_extract_list.append(self.smooth_window_size_input)
        self.disabled_when_extract_list.append(self.resolution_input)
        layout_smooth_window_size = QHBoxLayout()
        layout_smooth_window_size.addWidget(self.smooth_window_size_label)
        layout_smooth_window_size.addWidget(self.smooth_window_size_input)
        layout_smooth_window_size.addWidget(self.resolution_label)
        layout_smooth_window_size.addWidget(self.resolution_input)
        layout_smooth_window_size.addWidget(self.resolution_limit_label)
        self.control_panel_layout.addLayout(layout_smooth_window_size)

        # === Extract Next Frame Button ===
        self.extract_next_frame_button = QPushButton('Extract Next Frame (debug, no data output)')
        self.disabled_when_extract_list.append(self.extract_next_frame_button)
        self.control_panel_layout.addWidget(self.extract_next_frame_button)

        # === Extract Button and Progress Bar ===
        self.extract_button = QPushButton('Extract')
        self.disabled_when_extract_list.append(self.extract_button)
        self.extract_progress_bar = QProgressBar()
        self.extract_progress_bar.setValue(0)
        extract_progress_layout = QHBoxLayout()
        extract_progress_layout.addWidget(self.extract_button)
        extract_progress_layout.addWidget(self.extract_progress_bar)
        self.control_panel_layout.addLayout(extract_progress_layout)

        # === Pause Extract Button ===
        self.pause_extract_button = QPushButton('Pause Extract')
        self.control_panel_layout.addWidget(self.pause_extract_button)
        self.pause_extract_button.setDisabled(True)

        # === Abort Extract Button ===
        self.abort_extract_button = QPushButton('Abort Extract')
        self.control_panel_layout.addWidget(self.abort_extract_button)
        self.abort_extract_button.setDisabled(True)

        # === Pause Extract and Add Obstacle Button ===
        self.add_obstacle_button = QPushButton('Add Obstacle')
        self.control_panel_layout.addWidget(self.add_obstacle_button)
        self.add_obstacle_button.setDisabled(True)

    def initializeFrames(self):
        self.current_frame = None
        self.current_frame_with_mask = None
        self.current_frame_with_flow = None
        self.current_frame_overlayed = None
        self.current_frame_field = None

    # === Essential to keep ratio of sub displays even when some of them are empty ===
    def scaleDisplayToFitWindow(self):
        for sub_display in self.sub_displays:
            sub_display.setMaximumHeight(self.main_display.height() // 4)
            sub_display.setMaximumWidth(self.main_display.width() // 5)
    
    def resizeEvent(self, event):
        self.scaleDisplayToFitWindow()
        self.refreshDisplay()

    def displayInformation(self, text):
        self.info_label.setText(text)
    
    def displayFrameInLabel(self, frame, label):
        image = QImage(frame, frame.shape[1], frame.shape[0], frame.strides[0], QImage.Format_RGB888)
        if label.width() / label.height() > image.width() / image.height():
            image = image.scaledToHeight(label.height())
        else:
            image = image.scaledToWidth(label.width())
        label.setPixmap(QPixmap.fromImage(image))

    def refreshDisplay(self):
        if self.main_display_index == 0:
            self.main_frame = self.current_frame
        elif self.main_display_index == 1:
            self.main_frame = self.current_frame_with_mask
        elif self.main_display_index == 2:
            self.main_frame = self.current_frame_with_flow
        elif self.main_display_index == 3:
            self.main_frame = self.current_frame_overlayed
        elif self.main_display_index == 4:
            self.main_frame = self.current_frame_field

        if self.main_frame is not None:
            self.displayFrameInLabel(self.main_frame, self.main_display)
        else:
            self.main_display.setText("Source\nDisplay")
        if self.current_frame is not None:
            self.displayFrameInLabel(self.current_frame, self.sub_displays[0])
        else:
            self.sub_displays[0].setText("Source\nDisplay")
        if self.current_frame_with_mask is not None:
            self.displayFrameInLabel(self.current_frame_with_mask, self.sub_displays[1])
        else:
            self.sub_displays[1].setText("Herd\nMask")
        if self.current_frame_with_flow is not None:
            self.displayFrameInLabel(self.current_frame_with_flow, self.sub_displays[2])
        else:
            self.sub_displays[2].setText("Optical\nFlow")
        if self.current_frame_overlayed is not None:
            self.displayFrameInLabel(self.current_frame_overlayed, self.sub_displays[3])
        else:
            self.sub_displays[3].setText("Overlayed\nDisplay")
        if self.current_frame_field is not None:
            self.displayFrameInLabel(self.current_frame_field, self.sub_displays[4])
        else:
            self.sub_displays[4].setText("Density\nField")

    def setMainDisplayIndex(self):
        self.main_display_index = self.set_button.index(self.sender())
        self.refreshDisplay()

    def openFile(self):
        file_dialog = QFileDialog()
        file_dialog.setFileMode(QFileDialog.ExistingFile)
        file_dialog.setNameFilter("Files (*.mp4 *.png *.jpg)")
        file_dialog.setViewMode(QFileDialog.Detail)
        
        if file_dialog.exec():
            file_path = file_dialog.selectedFiles()[0]
            current_dir = QDir.current()
            relative_path = current_dir.relativeFilePath(file_path)
            self.source_file_label.setText(relative_path)
            self.source_file_path = relative_path

            self.extraction_processor = ExtractionProcessor(self.source_file_path, self)
            self.current_frame = self.extraction_processor.getPreviewFrame()
            self.current_frame_with_mask = self.extraction_processor.getPreviewFrameMasked(self.body_color, self.color_diff_threshold)
            self.current_frame_with_flow = None
            self.current_frame_overlayed = None
            self.current_frame_field = None

            self.resolution_limit = self.extraction_processor.getResolutionLimit(self.body_length)
            self.resolution_limit_label.setText("< " + str(self.resolution_limit))

            self.refreshDisplay()

    def pickBodyColorByOpenCV(self):
        img = cv.cvtColor(self.current_frame, cv.COLOR_RGB2BGR)
        img = ResizeImageLargerSideTo(img, 700)
        img_text = cv.putText(img, "Find an area to enlarge to pick the agent color", (10, 20), cv.FONT_HERSHEY_DUPLEX, 0.5, (0, 255, 0), 1, cv.LINE_AA)
        img_text = cv.putText(img_text, "Select a ROI and then press SPACE or ENTER button!", (10, 35), cv.FONT_HERSHEY_DUPLEX, 0.5, (0, 255, 0), 1, cv.LINE_AA)
        img_text = cv.putText(img_text, "Cancel the selection process by pressing c button!", (10, 50), cv.FONT_HERSHEY_DUPLEX, 0.5, (0, 255, 0), 1, cv.LINE_AA)
        box = cv.selectROI("Pick agent color", img_text, fromCenter=False, showCrosshair=True)
        if box[2] == 0 or box[3] == 0:
            cv.destroyWindow("Pick agent color")
            return

        single_agent = img[box[1]:box[1] + box[3], box[0]:box[0] + box[2]]
        single_agent = ResizeImageLargerSideTo(single_agent, 700)
        single_agent_text = cv.putText(single_agent, "Choose an area and the average color ", (10, 20), cv.FONT_HERSHEY_DUPLEX, 0.5, (0, 255, 0), 1, cv.LINE_AA)
        single_agent_text = cv.putText(single_agent_text, "in the area is used as the agent color", (10, 35), cv.FONT_HERSHEY_DUPLEX, 0.5, (0, 255, 0), 1, cv.LINE_AA)
        box = cv.selectROI("Pick agent color", single_agent_text, fromCenter=False, showCrosshair=True)
        if box[2] == 0 or box[3] == 0:
            cv.destroyWindow("Pick agent color")
            return
        
        single_agent = single_agent[box[1]:box[1] + box[3], box[0]:box[0] + box[2]]
        agent_color = np.mean(single_agent, axis=(0, 1))
        print("Agent color: ", agent_color) 
        cv.destroyWindow("Pick agent color")

        self.body_color = [agent_color[2], agent_color[1], agent_color[0]]
        self.body_color_r_input.setText(str(int(agent_color[2])))
        self.body_color_g_input.setText(str(int(agent_color[1])))
        self.body_color_b_input.setText(str(int(agent_color[0])))
        self.body_color_label.changeColor(QColor(int(agent_color[2]), int(agent_color[1]), int(agent_color[0])))
        self.current_frame_with_mask = self.extraction_processor.getPreviewFrameMasked(self.body_color, self.color_diff_threshold)
        self.refreshDisplay()

    def enterBodyColor(self):
        r = int(self.body_color_r_input.text())
        g = int(self.body_color_g_input.text())
        b = int(self.body_color_b_input.text())
        self.body_color = [r, g, b]
        self.body_color_label.changeColor(QColor(r, g, b))
        self.current_frame_with_mask = self.extraction_processor.getPreviewFrameMasked(self.body_color, self.color_diff_threshold)
        self.refreshDisplay()

    def setColorDiffThreshold(self, value):
        self.color_diff_threshold_label.setText("Color Threshold: " + str(value))
        self.color_diff_threshold = value
        self.current_frame_with_mask = self.extraction_processor.getPreviewFrameMasked(self.body_color, self.color_diff_threshold)
        self.refreshDisplay()

    def pickBodyPixelCoverageRegionByOpenCV(self):
        # img = cv.cvtColor(self.current_frame, cv.COLOR_RGB2BGR)
        img = cv.cvtColor(self.extraction_processor.getPreviewFrameStabilized(), cv.COLOR_RGB2BGR)
        new_length = 1000
        longer_side = max(img.shape[0], img.shape[1])
        scale = new_length / longer_side
        img_resized = cv.resize(img, (int(math.floor(img.shape[1] * scale)), int(math.floor(img.shape[0] * scale))))
        img_text = cv.putText(img_resized, "Find an area with approximately 10 agents", (10, 30), cv.FONT_HERSHEY_DUPLEX, 0.7, (0, 255, 0), 1, cv.LINE_AA)
        box = cv.selectROI("Choose Region", img_text, fromCenter=False, showCrosshair=True)
        choosedRegion = img[int(box[1] / scale):int((box[1] + box[3]) / scale), int(box[0] / scale):int((box[0] + box[2]) / scale)]
        self.body_pixel_coverage_window.setChoosedRegion(choosedRegion, self.body_color, self.color_diff_threshold)
        self.body_pixel_coverage_window.agent_num_enter_field.clear()
        cv.destroyWindow("Choose Region")
        self.body_pixel_coverage_window.show()

    def confirmBodyPixelCoverage(self):
        self.body_pixel_coverage = self.body_pixel_coverage_window.body_pixel_coverage
        self.body_pixel_coverage_input.setText(str(self.body_pixel_coverage))
        self.body_length = self.body_pixel_coverage_window.body_length
        self.body_length_input.setText(str(self.body_length))
        self.body_pixel_coverage_window.hide()
        self.displayInformation("Body Pixel Coverage is set to " + str(self.body_pixel_coverage) + "\nand Body Length is set to " + str(self.body_length))

    def setBodyPixelCoverageByInput(self):
        self.body_pixel_coverage = int(self.body_pixel_coverage_input.text())
        self.displayInformation("Body Pixel Coverage is set to " + str(self.body_pixel_coverage))

    def setBodyLength(self):
        self.body_length = int(self.body_length_input.text())
        self.resolution_limit = self.extraction_processor.getResolutionLimit(self.body_length)
        self.resolution_limit_label.setText("< " + str(self.resolution_limit))
        self.displayInformation("Body Length is set to " + str(self.body_length))

    def setOverlayArrowMultiplier(self, value):
        self.extraction_processor.setOverlayArrowMultiplier(value)
        self.overlay_arrow_multiplier_label.setText("Overlay Arrow Multiplier: " + str(value))
        self.refreshDisplay()

    def setConstantTotalBodyPixelCount(self):
        self.is_constant_total_body_pixel_count = self.constant_total_body_pixel_count_checkbox.isChecked()
        self.displayInformation("Constant Total Body Pixel Count is set to " + str(self.is_constant_total_body_pixel_count))

    def setSmoothWindowSize(self):
        self.smooth_window_size = int(self.smooth_window_size_input.text())
        self.displayInformation("Smooth Window Size is set to " + str(self.smooth_window_size))

    def setResolution(self):
        self.resolution = int(self.resolution_input.text())
        self.displayInformation("Resolution is set to " + str(self.resolution))

    def extractNextFrame(self):
        if not self.extraction_processor.is_extraction_initialized:
            overlay_arrow_multiplier = self.overlay_arrow_multiplier_slider.value()
            self.extraction_processor.initializeExtraction(
                self.body_color, self.color_diff_threshold, 
                self.body_pixel_coverage, self.body_length, 
                self.smooth_window_size, self.resolution, 
                overlay_arrow_multiplier, self.is_constant_total_body_pixel_count
            )

        startTime = time.time()
        if self.extraction_processor.extractNextFrame():
            self.current_frame = self.extraction_processor.getCurrentFrame()
            self.current_frame_with_mask = self.extraction_processor.getCurrentFrameWithMask()
            self.current_frame_with_flow = self.extraction_processor.getOpticalFlowRGB()
            self.current_frame_overlayed = self.extraction_processor.getOverlayedFrame()
            self.current_frame_field = self.extraction_processor.getDensityField()
            self.refreshDisplay()
            self.displayInformation("Last frame extracted in " + str(time.time() - startTime) + " seconds")

    def extract(self):
        # Reset the video processor
        self.extraction_processor = ExtractionProcessor(self.source_file_path, self)
        overlay_arrow_multiplier = self.overlay_arrow_multiplier_slider.value()
        self.extraction_processor.initializeExtraction(
            self.body_color, self.color_diff_threshold, 
            self.body_pixel_coverage, self.body_length, 
            self.smooth_window_size, self.resolution, 
            overlay_arrow_multiplier, self.is_constant_total_body_pixel_count
        )

        # Disable GUI elements
        for widget in self.disabled_when_extract_list:
            widget.setDisabled(True)
        self.extract_progress_bar.setMaximum(self.extraction_processor.getTotalFrameCount())
        self.extract_progress_bar.setValue(0)

        # Open a background thread to extract the video
        self.background_thread = QThreadPool.globalInstance()
        self.is_extracting = True
        video_process_thread = VideoProcessThread(self.extraction_processor, self)
        video_process_thread.signals.frame_ready.connect(self.displayExtractedNextFrame)
        video_process_thread.signals.finished.connect(self.extractFinished)
        self.background_thread.start(video_process_thread)

        self.pause_extract_button.setDisabled(False)
        self.abort_extract_button.setDisabled(False)
        self.add_obstacle_button.setDisabled(False)

    def pauseExtract(self):
        if self.is_extracting:
            self.is_extracting = False
            self.pause_extract_button.setText("Resume Extract")
            self.displayInformation("Extraction paused")
        else:
            self.is_extracting = True
            video_process_thread = VideoProcessThread(self.extraction_processor, self)
            video_process_thread.signals.frame_ready.connect(self.displayExtractedNextFrame)
            video_process_thread.signals.finished.connect(self.extractFinished)
            self.background_thread.start(video_process_thread)
            self.pause_extract_button.setText("Pause Extract")
            self.displayInformation("Extraction resumed")

    def abortExtract(self):
        self.is_extracting = False
        self.background_thread.waitForDone()
        if hasattr(self, 'extraction_processor') and self.extraction_processor:
                self.extraction_processor.release_writers()
        self.displayInformation("Extraction aborted")
        for widget in self.disabled_when_extract_list:
            widget.setDisabled(False)
        self.pause_extract_button.setDisabled(True)
        self.abort_extract_button.setDisabled(True)
        self.add_obstacle_button.setDisabled(True)
        self.extract_progress_bar.setValue(0)

    def displayExtractedNextFrame(self):
        self.extract_progress_bar.setValue(self.extraction_processor.getCurrentFrameCount())
        self.current_frame = self.extraction_processor.getCurrentFrame()
        self.current_frame_with_mask = self.extraction_processor.getCurrentFrameWithMask()
        self.current_frame_with_flow = self.extraction_processor.getOpticalFlowRGB()
        self.current_frame_overlayed = self.extraction_processor.getOverlayedFrame()
        self.current_frame_field = self.extraction_processor.getDensityField()
        self.refreshDisplay()

    def extractFinished(self):
        self.is_extracting = False
        if hasattr(self, 'extraction_processor') and self.extraction_processor:
            self.extraction_processor.release_writers()
        self.extract_progress_bar.setValue(self.extraction_processor.getTotalFrameCount())
        self.displayInformation("Extraction finished")
        for widget in self.disabled_when_extract_list:
            widget.setDisabled(False)
        self.pause_extract_button.setDisabled(True)
        self.abort_extract_button.setDisabled(True)
        self.add_obstacle_button.setDisabled(True)

    def addObstacle(self):
        # When extracting, pause the extraction and add obstacle   
        if self.is_extracting and self.extraction_processor.is_extraction_initialized:
            self.is_extracting = False
            overlay = self.current_frame_overlayed.copy()
            overlay = cv.cvtColor(overlay, cv.COLOR_BGR2RGB)
            box = cv.selectROI("Choose Obstacle", overlay, fromCenter=False, showCrosshair=True)
            if (box[2] > 0) and (box[3] > 0):
                self.extraction_processor.addObstacle(box)
            cv.destroyWindow("Choose Obstacle")
            self.is_extracting = True
            video_process_thread = VideoProcessThread(self.extraction_processor, self)
            video_process_thread.signals.frame_ready.connect(self.displayExtractedNextFrame)
            video_process_thread.signals.finished.connect(self.extractFinished)
            self.background_thread.start(video_process_thread)
        # When not extracting, add obstacle directly
        if not self.is_extracting and self.extraction_processor.is_extraction_initialized:
            overlay = self.current_frame_overlayed.copy()
            overlay = cv.cvtColor(overlay, cv.COLOR_BGR2RGB)
            box = cv.selectROI("Choose Obstacle", overlay, fromCenter=False, showCrosshair=True)
            if (box[2] > 0) and (box[3] > 0):
                self.extraction_processor.addObstacle(box)
            cv.destroyWindow("Choose Obstacle")
            self.current_frame_overlayed = self.extraction_processor.getOverlayedFrame()
            self.current_frame_field = self.extraction_processor.getDensityField()
            self.refreshDisplay()

    # === Initialize event calls for GUI widgets ===
    def initializeSlots(self):
        for i in range(5):
            self.set_button[i].clicked.connect(self.setMainDisplayIndex)

        self.open_file_button.clicked.connect(self.openFile)

        self.pick_body_color_button.clicked.connect(self.pickBodyColorByOpenCV)
        self.body_color_r_input.editingFinished.connect(self.enterBodyColor)
        self.body_color_g_input.editingFinished.connect(self.enterBodyColor)
        self.body_color_b_input.editingFinished.connect(self.enterBodyColor)
        self.color_diff_threshold_slider.valueChanged.connect(self.setColorDiffThreshold)

        self.body_pixel_coverage_button.clicked.connect(self.pickBodyPixelCoverageRegionByOpenCV)
        self.body_pixel_coverage_window.confirm_button.clicked.connect(self.confirmBodyPixelCoverage)
        self.body_pixel_coverage_input.editingFinished.connect(self.setBodyPixelCoverageByInput)
        self.body_length_input.editingFinished.connect(self.setBodyLength)

        self.overlay_arrow_multiplier_slider.valueChanged.connect(self.setOverlayArrowMultiplier)
        self.constant_total_body_pixel_count_checkbox.stateChanged.connect(self.setConstantTotalBodyPixelCount)
        self.smooth_window_size_input.editingFinished.connect(self.setSmoothWindowSize)
        self.resolution_input.editingFinished.connect(self.setResolution)

        self.extract_next_frame_button.clicked.connect(self.extractNextFrame)
        self.extract_button.clicked.connect(self.extract)
        self.pause_extract_button.clicked.connect(self.pauseExtract)
        self.abort_extract_button.clicked.connect(self.abortExtract)
        self.add_obstacle_button.clicked.connect(self.addObstacle)


# === Main Body of the Program === #
app = QApplication([])

window = MainWindow()
window.show()
window.resize(1000, 600)

app.exec()
# ================================ #