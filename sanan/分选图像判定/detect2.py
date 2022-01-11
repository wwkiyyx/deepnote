import argparse
import time
import os
import shutil
from pathlib import Path

import pymysql

import cv2
import numpy as np
import torch
import torch.backends.cudnn as cudnn

from models.experimental import attempt_load
from utils.datasets import LoadStreams, LoadImages
from utils.general import check_img_size, check_requirements, check_imshow, non_max_suppression, apply_classifier, \
    scale_coords, xyxy2xywh, strip_optimizer, set_logging, increment_path, save_one_box
from utils.plots import colors, plot_one_box
from utils.torch_utils import select_device, load_classifier, time_synchronized


@torch.no_grad()
def detect(model, opt):
    source, weights, view_img, save_txt, imgsz = opt.source, opt.weights, opt.view_img, opt.save_txt, opt.img_size
    save_img = not opt.nosave and not source.endswith('.txt')  # save inference images
    webcam = source.isnumeric() or source.endswith('.txt') or source.lower().startswith(
        ('rtsp://', 'rtmp://', 'http://', 'https://'))

    # Directories
    save_dir = increment_path(Path(opt.project) / opt.name, exist_ok=opt.exist_ok)  # increment run
    (save_dir / 'labels' if save_txt else save_dir).mkdir(parents=True, exist_ok=True)  # make dir

    # Initialize
    set_logging()
    device = select_device(opt.device)
    half = device.type != 'cpu'  # half precision only supported on CUDA

    # Load model
    # model = attempt_load(weights, map_location=device)  # load FP32 model
    stride = int(model.stride.max())  # model stride
    imgsz = check_img_size(imgsz, s=stride)  # check img_size
    names = model.module.names if hasattr(model, 'module') else model.names  # get class names
    # if half:
    #    model.half()  # to FP16

    # Second-stage classifier
    classify = False
    if classify:
        modelc = load_classifier(name='resnet101', n=2)  # initialize
        modelc.load_state_dict(torch.load('weights/resnet101.pt', map_location=device)['model']).to(device).eval()

    # Set Dataloader
    vid_path, vid_writer = None, None
    if webcam:
        view_img = check_imshow()
        cudnn.benchmark = True  # set True to speed up constant image size inference
        dataset = LoadStreams(source, img_size=imgsz, stride=stride)
    else:
        dataset = LoadImages(source, img_size=imgsz, stride=stride)

    # Run inference
    if device.type != 'cpu':
        model(torch.zeros(1, 3, imgsz, imgsz).to(device).type_as(next(model.parameters())))  # run once
    t0 = time.time()
    for path, img, im0s, vid_cap in dataset:
        img = torch.from_numpy(img).to(device)
        img = img.half() if half else img.float()  # uint8 to fp16/32
        img /= 255.0  # 0 - 255 to 0.0 - 1.0
        if img.ndimension() == 3:
            img = img.unsqueeze(0)

        # Inference
        t1 = time_synchronized()
        pred = model(img, augment=opt.augment)[0]

        # Apply NMS
        pred = non_max_suppression(pred, opt.conf_thres, opt.iou_thres, opt.classes, opt.agnostic_nms,
                                   max_det=opt.max_det)
        t2 = time_synchronized()

        # Apply Classifier
        if classify:
            pred = apply_classifier(pred, modelc, img, im0s)

        # Process detections
        for i, det in enumerate(pred):  # detections per image
            if webcam:  # batch_size >= 1
                p, s, im0, frame = path[i], f'{i}: ', im0s[i].copy(), dataset.count
            else:
                p, s, im0, frame = path, '', im0s.copy(), getattr(dataset, 'frame', 0)

            p = Path(p)  # to Path
            save_path = str(save_dir / p.name)  # img.jpg
            txt_path = str(save_dir / 'labels' / p.stem) + ('' if dataset.mode == 'image' else f'_{frame}')  # img.txt
            s += '%gx%g ' % img.shape[2:]  # print string
            gn = torch.tensor(im0.shape)[[1, 0, 1, 0]]  # normalization gain whwh
            imc = im0.copy() if opt.save_crop else im0  # for opt.save_crop
            if len(det):
                # Rescale boxes from img_size to im0 size
                det[:, :4] = scale_coords(img.shape[2:], det[:, :4], im0.shape).round()

                # Print results
                for c in det[:, -1].unique():
                    n = (det[:, -1] == c).sum()  # detections per class
                    s += f"{n} {names[int(c)]}{'s' * (n > 1)}, "  # add to string

                # Write results
                for *xyxy, conf, cls in reversed(det):
                    if save_txt:  # Write to file
                        xywh = (xyxy2xywh(torch.tensor(xyxy).view(1, 4)) / gn).view(-1).tolist()  # normalized xywh
                        line = (cls, *xywh, conf) if opt.save_conf else (cls, *xywh)  # label format
                        with open(txt_path + '.txt', 'a') as f:
                            f.write(('%g ' * len(line)).rstrip() % line + '\n')

                    if save_img or opt.save_crop or view_img:  # Add bbox to image
                        c = int(cls)  # integer class
                        label = None if opt.hide_labels else (names[c] if opt.hide_conf else f'{names[c]} {conf:.2f}')
                        plot_one_box(xyxy, im0, label=label, color=colors(c, True), line_thickness=opt.line_thickness)
                        if opt.save_crop:
                            save_one_box(xyxy, imc, file=save_dir / 'crops' / names[c] / f'{p.stem}.jpg', BGR=True)

            # Print time (inference + NMS)
            print(f'{s}Done. ({t2 - t1:.3f}s)')

            # Stream results
            if view_img:
                cv2.imshow(str(p), im0)
                cv2.waitKey(1)  # 1 millisecond

            # Save results (image with detections)
            if save_img:
                if dataset.mode == 'image':
                    cv2.imwrite(save_path, im0)
                else:  # 'video' or 'stream'
                    if vid_path != save_path:  # new video
                        vid_path = save_path
                        if isinstance(vid_writer, cv2.VideoWriter):
                            vid_writer.release()  # release previous video writer
                        if vid_cap:  # video
                            fps = vid_cap.get(cv2.CAP_PROP_FPS)
                            w = int(vid_cap.get(cv2.CAP_PROP_FRAME_WIDTH))
                            h = int(vid_cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
                        else:  # stream
                            fps, w, h = 30, im0.shape[1], im0.shape[0]
                            save_path += '.mp4'
                        vid_writer = cv2.VideoWriter(save_path, cv2.VideoWriter_fourcc(*'mp4v'), fps, (w, h))
                    vid_writer.write(im0)

    if save_txt or save_img:
        s = f"\n{len(list(save_dir.glob('labels/*.txt')))} labels saved to {save_dir / 'labels'}" if save_txt else ''
        print(f"Results saved to {save_dir}{s}")

    print(f'Done. ({time.time() - t0:.3f}s)')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--weights', nargs='+', type=str, default='yolov3.pt', help='model.pt path(s)')
    parser.add_argument('--source', type=str, default='data/images', help='source')  # file/folder, 0 for webcam
    parser.add_argument('--img-size', type=int, default=640, help='inference size (pixels)')
    parser.add_argument('--conf-thres', type=float, default=0.25, help='object confidence threshold')
    parser.add_argument('--iou-thres', type=float, default=0.45, help='IOU threshold for NMS')
    parser.add_argument('--max-det', type=int, default=1000, help='maximum number of detections per image')
    parser.add_argument('--device', default='', help='cuda device, i.e. 0 or 0,1,2,3 or cpu')
    parser.add_argument('--view-img', action='store_true', help='display results')
    parser.add_argument('--save-txt', action='store_true', help='save results to *.txt')
    parser.add_argument('--save-conf', action='store_true', help='save confidences in --save-txt labels')
    parser.add_argument('--save-crop', action='store_true', help='save cropped prediction boxes')
    parser.add_argument('--nosave', action='store_true', help='do not save images/videos')
    parser.add_argument('--classes', nargs='+', type=int, help='filter by class: --class 0, or --class 0 2 3')
    parser.add_argument('--agnostic-nms', action='store_true', help='class-agnostic NMS')
    parser.add_argument('--augment', action='store_true', help='augmented inference')
    parser.add_argument('--update', action='store_true', help='update all models')
    parser.add_argument('--project', default='runs/detect', help='save results to project/name')
    parser.add_argument('--name', default='exp', help='save results to project/name')
    parser.add_argument('--exist-ok', action='store_true', help='existing project/name ok, do not increment')
    parser.add_argument('--line-thickness', default=3, type=int, help='bounding box thickness (pixels)')
    parser.add_argument('--hide-labels', default=False, action='store_true', help='hide labels')
    parser.add_argument('--hide-conf', default=False, action='store_true', help='hide confidences')
    opt = parser.parse_args()
    print(opt)
    check_requirements(exclude=('tensorboard', 'pycocotools', 'thop'))

    if opt.update:  # update all models (to fix SourceChangeWarning)
        print('opt.update is true')
        for opt.weights in ['yolov3.pt', 'yolov3-spp.pt', 'yolov3-tiny.pt']:
            detect(opt=opt)
            strip_optimizer(opt.weights)
    else:
        host = 'localhost'
        user = 'root'
        password = '880510'
        db = 'fx'
        sql_select = "SELECT * FROM fileTmpTest"
        sql_delete1 = "DELETE FROM fileTmpTest WHERE file_name = "
        sql_delete2 = "DELETE FROM resFile WHERE file_name = "
        sql_insert = "INSERT INTO resFile(file_datetime, file_name, lot, dt, d, res_dir, res, res_ch, x) VALUES "
        sql_update1 = "UPDATE resFile SET "
        sql_update2 = " WHERE file_name = "
        print('opt.update is false')
        opt.line_thickness = 1
        opt.save_txt = True
        opt.save_conf = True
        opt.hide_labels = True
        opt.hide_conf = True
        opt.name = 'result'
        device = select_device(opt.device)
        half = device.type != 'cpu'
        model = attempt_load(opt.weights, map_location=device)
        if half:
            model.half()
        res_dir = 'D:\\web\\res'
        while True:
            source = []
            # dates = ["2021-12-10", "2021-12-09", "2021-12-08", "2021-12-07"]
            # for day in dates:
            #    src_images = os.listdir("D:\\web\\src\\" + day)
            #    for src_image in src_images:
            #        source.append("D:\\web\\src\\" + day + "\\" + src_image)
            conn = pymysql.connect(host=host, user=user, password=password, database=db)
            cursor = conn.cursor()
            # tick = time.strftime("%Y-%m-%d %H:%M:%S ", time.localtime()) + sql_select
            # if ":11 " in tick:
            #     print(tick)
            cursor.execute("select file_name from resFile where (d = '2021-12-13' or d = '2021-12-14' or \
                            d = '2021-12-15' or d = '2021-12-16' or d = '2021-12-17' or d = '2021-12-18' \
                            or d = '2021-12-19') \
                            and res = 'NG'")
                            # and res_dir like '%V2N21B260236-23_128_-527_211214084019'")

            results = cursor.fetchall()
            for row in results:
                source.append(row[0])
                print(row[0])
            # conn.close()
            # break
            # conn = pymysql.connect(host=host, user=user, password=password, database=db)
            # cursor = conn.cursor()
            # for image in source:
            #     cursor.execute(sql_delete2 + '\'' + image.replace('\\', '\\\\') + '\'')
            #     conn.commit()
            # conn.close()
            # conn = pymysql.connect(host=host, user=user, password=password, database=db)
            # cursor = conn.cursor()
            for image in source:
                try:
                    name = image[image.rindex('\\')+1:len(image)]
                    lot = name[0:name.index('_')]
                    dt = name[name.rindex('_')+1:name.rindex('_')+13]
                    date = '20'+dt[0:2]+'-'+dt[2:4]+'-'+dt[4:6]
                    mat = cv2.imread(image, cv2.IMREAD_COLOR)
                    h, w, c = mat.shape
                    if h > 1000:
                        h = 980
                    color = mat[105:585, 20:450]
                    chip = mat[640:h-20, 70:470]
                    chip_h, chip_w, chip_c = chip.shape
                    b, g, r = cv2.split(chip)
                    # g3 = cv2.multiply(g, 3)
                    kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (3, 3))
                    chip_src = cv2.morphologyEx(g, cv2.MORPH_CLOSE, kernel)
                    chip_line = cv2.absdiff(b, r)
                    chip_x = chip_line[h-710:h-660, 0:400]
                    x_min_val, x_max_val, x_min_index, x_max_index = cv2.minMaxLoc(chip_x)
                    chip_y = chip_line[0:h-660, 0:50]
                    y_min_val, y_max_val, y_min_index, y_max_index = cv2.minMaxLoc(chip_y)
                    chip_center = (x_max_index[0], y_max_index[1])
                    if not os.path.exists(res_dir):
                        os.mkdir(res_dir)
                    if not os.path.exists(res_dir + '\\' + date):
                        os.mkdir(res_dir + '\\' + date)
                    if not os.path.exists(res_dir + '\\' + date + '\\' + lot):
                        os.mkdir(res_dir + '\\' + date + '\\' + lot)
                    if not os.path.exists(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '')):
                        os.mkdir(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', ''))
                    if os.path.exists(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\result'):
                        shutil.rmtree(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\result')
                    cv2.imwrite(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\chip.jpg', chip_src)
                    opt.source = res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\chip.jpg'
                    opt.project = res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '')
                    print(time.strftime("%Y-%m-%d %H:%M:%S ", time.localtime()) + image)
                    detect(model, opt=opt)
                    yolo_class = []
                    yolo_rects = []
                    yolo_scores = []
                    if os.path.exists(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\result\\labels\\chip.txt'):
                        chip_txt = open(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\result\\labels\\chip.txt', "r")
                        for line in chip_txt.readlines():
                            if len(line) > 10:
                                datas = line.split(' ')
                                yolo_class.append(datas[0])
                                yolo_scores.append(datas[5])
                                c_x = chip_w * float(datas[1])
                                c_y = chip_h * float(datas[2])
                                r_w = chip_w * float(datas[3])
                                r_h = chip_h * float(datas[4])
                                r_x = c_x - r_w / 2
                                r_y = c_y - r_h / 2
                                rect = (r_x, r_y, r_w, r_h)
                                yolo_rects.append(rect)
                        chip_txt.close()
                    yolo_center = False
                    yolo_center_index = 0
                    for r in yolo_rects:
                        if r[0] < chip_center[0] and r[1] < chip_center[1] and r[0] + r[2] > chip_center[0] and r[1] + r[3] > chip_center[1]:
                            yolo_center = True
                            break
                        yolo_center_index += 1
                    result = open(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\result.txt', mode="w")
                    color_gray = cv2.cvtColor(color, cv2.COLOR_BGR2GRAY)
                    ret_color, color_threshold = cv2.threshold(color_gray, 230, 255, cv2.THRESH_BINARY)
                    color_b, color_g, color_r = cv2.split(color)
                    color_gray = cv2.add(color_gray, color_b)
                    pink = cv2.inRange(color, np.array([150, 0, 150]), np.array([255, 150, 255]))
                    color_gray = cv2.subtract(color_gray, pink)
                    white = cv2.inRange(color, np.array([200, 200, 200]), np.array([255, 255, 255]))
                    color_gray = cv2.subtract(color_gray, white)
                    # if yolo_center:
                    #     if yolo_class[yolo_center_index] == 2 or yolo_class[yolo_center_index] == 3:
                    #         pink = cv2.InRange(color, np.array([150, 0, 150]), np.array([255, 150, 255]))
                    #         color_gray = cv2.subtract(color_gray, pink)
                    #     elif yolo_class[yolo_center_index] == 0:
                    #         white = cv2.InRange(color, np.array([200, 200, 200]), np.array([255, 255, 255]))
                    #         color_gray = cv2.subtract(color_gray, white)
                    rect_gray = cv2.imread("rect.bmp", cv2.IMREAD_GRAYSCALE)
                    rect_h, rect_w = rect_gray.shape
                    ret_rect, rect_threshold = cv2.threshold(rect_gray, 128, 255, cv2.THRESH_BINARY)
                    match = cv2.matchTemplate(color_threshold, rect_threshold, cv2.TM_SQDIFF)
                    match_min_val, match_max_val, match_min_index, match_max_index = cv2.minMaxLoc(match)
                    rect_min = (int(match_min_index[0] + rect_w / 2), int(match_min_index[1] + rect_h / 2))
                    color_x3 = color[rect_min[1]-7:rect_min[1]+8, rect_min[0]-7:rect_min[0]+8]
                    color_x3_200 = cv2.resize(color_x3, (200, 200))
                    color_gray_x3_1_1 = color_gray[rect_min[1]-6:rect_min[1]-3, rect_min[0]-6:rect_min[0]-3]
                    color_gray_x3_1_2 = color_gray[rect_min[1]-6:rect_min[1]-3, rect_min[0]-1:rect_min[0]+2]
                    color_gray_x3_1_3 = color_gray[rect_min[1]-6:rect_min[1]-3, rect_min[0]+4:rect_min[0]+7]
                    color_gray_x3_2_1 = color_gray[rect_min[1]-1:rect_min[1]+2, rect_min[0]-6:rect_min[0]-3]
                    color_gray_x3_2_2 = color_gray[rect_min[1]-1:rect_min[1]+2, rect_min[0]-1:rect_min[0]+2]
                    color_gray_x3_2_3 = color_gray[rect_min[1]-1:rect_min[1]+2, rect_min[0]+4:rect_min[0]+7]
                    color_gray_x3_3_1 = color_gray[rect_min[1]+4:rect_min[1]+7, rect_min[0]-6:rect_min[0]-3]
                    color_gray_x3_3_2 = color_gray[rect_min[1]+4:rect_min[1]+7, rect_min[0]-1:rect_min[0]+2]
                    color_gray_x3_3_3 = color_gray[rect_min[1]+4:rect_min[1]+7, rect_min[0]+4:rect_min[0]+7]
                    color_gray_x3_1_1_mean = color_gray_x3_1_1.mean()
                    color_gray_x3_1_1_min_val, color_gray_x3_1_1_max_val, color_gray_x3_1_1_min_index, color_gray_x3_1_1_max_index = cv2.minMaxLoc(color_gray_x3_1_1)
                    color_gray_x3_1_2_mean = color_gray_x3_1_2.mean()
                    color_gray_x3_1_2_min_val, color_gray_x3_1_2_max_val, color_gray_x3_1_2_min_index, color_gray_x3_1_2_max_index = cv2.minMaxLoc(color_gray_x3_1_2)
                    color_gray_x3_1_3_mean = color_gray_x3_1_3.mean()
                    color_gray_x3_1_3_min_val, color_gray_x3_1_3_max_val, color_gray_x3_1_3_min_index, color_gray_x3_1_3_max_index = cv2.minMaxLoc(color_gray_x3_1_3)
                    color_gray_x3_2_1_mean = color_gray_x3_2_1.mean()
                    color_gray_x3_2_1_min_val, color_gray_x3_2_1_max_val, color_gray_x3_2_1_min_index, color_gray_x3_2_1_max_index = cv2.minMaxLoc(color_gray_x3_2_1)
                    color_gray_x3_2_2_mean = color_gray_x3_2_2.mean()
                    color_gray_x3_2_2_min_val, color_gray_x3_2_2_max_val, color_gray_x3_2_2_min_index, color_gray_x3_2_2_max_index = cv2.minMaxLoc(color_gray_x3_2_2)
                    color_gray_x3_2_3_mean = color_gray_x3_2_3.mean()
                    color_gray_x3_2_3_min_val, color_gray_x3_2_3_max_val, color_gray_x3_2_3_min_index, color_gray_x3_2_3_max_index = cv2.minMaxLoc(color_gray_x3_2_3)
                    color_gray_x3_3_1_mean = color_gray_x3_3_1.mean()
                    color_gray_x3_3_1_min_val, color_gray_x3_3_1_max_val, color_gray_x3_3_1_min_index, color_gray_x3_3_1_max_index = cv2.minMaxLoc(color_gray_x3_3_1)
                    color_gray_x3_3_2_mean = color_gray_x3_3_2.mean()
                    color_gray_x3_3_2_min_val, color_gray_x3_3_2_max_val, color_gray_x3_3_2_min_index, color_gray_x3_3_2_max_index = cv2.minMaxLoc(color_gray_x3_3_2)
                    color_gray_x3_3_3_mean = color_gray_x3_3_3.mean()
                    color_gray_x3_3_3_min_val, color_gray_x3_3_3_max_val, color_gray_x3_3_3_min_index, color_gray_x3_3_3_max_index = cv2.minMaxLoc(color_gray_x3_3_3)
                    pink_x3_1_1 = pink[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] - 6:rect_min[0] - 3]
                    pink_x3_1_2 = pink[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] - 1:rect_min[0] + 2]
                    pink_x3_1_3 = pink[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] + 4:rect_min[0] + 7]
                    pink_x3_2_1 = pink[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] - 6:rect_min[0] - 3]
                    pink_x3_2_2 = pink[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] - 1:rect_min[0] + 2]
                    pink_x3_2_3 = pink[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] + 4:rect_min[0] + 7]
                    pink_x3_3_1 = pink[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] - 6:rect_min[0] - 3]
                    pink_x3_3_2 = pink[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] - 1:rect_min[0] + 2]
                    pink_x3_3_3 = pink[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] + 4:rect_min[0] + 7]
                    pink_x3_1_1_mean = pink_x3_1_1.mean()
                    pink_x3_1_1_min_val, pink_x3_1_1_max_val, pink_x3_1_1_min_index, pink_x3_1_1_max_index = cv2.minMaxLoc(pink_x3_1_1)
                    pink_x3_1_2_mean = pink_x3_1_2.mean()
                    pink_x3_1_2_min_val, pink_x3_1_2_max_val, pink_x3_1_2_min_index, pink_x3_1_2_max_index = cv2.minMaxLoc(pink_x3_1_2)
                    pink_x3_1_3_mean = pink_x3_1_3.mean()
                    pink_x3_1_3_min_val, pink_x3_1_3_max_val, pink_x3_1_3_min_index, pink_x3_1_3_max_index = cv2.minMaxLoc(pink_x3_1_3)
                    pink_x3_2_1_mean = pink_x3_2_1.mean()
                    pink_x3_2_1_min_val, pink_x3_2_1_max_val, pink_x3_2_1_min_index, pink_x3_2_1_max_index = cv2.minMaxLoc(pink_x3_2_1)
                    pink_x3_2_2_mean = pink_x3_2_2.mean()
                    pink_x3_2_2_min_val, pink_x3_2_2_max_val, pink_x3_2_2_min_index, pink_x3_2_2_max_index = cv2.minMaxLoc(pink_x3_2_2)
                    pink_x3_2_3_mean = pink_x3_2_3.mean()
                    pink_x3_2_3_min_val, pink_x3_2_3_max_val, pink_x3_2_3_min_index, pink_x3_2_3_max_index = cv2.minMaxLoc(pink_x3_2_3)
                    pink_x3_3_1_mean = pink_x3_3_1.mean()
                    pink_x3_3_1_min_val, pink_x3_3_1_max_val, pink_x3_3_1_min_index, pink_x3_3_1_max_index = cv2.minMaxLoc(pink_x3_3_1)
                    pink_x3_3_2_mean = pink_x3_3_2.mean()
                    pink_x3_3_2_min_val, pink_x3_3_2_max_val, pink_x3_3_2_min_index, pink_x3_3_2_max_index = cv2.minMaxLoc(pink_x3_3_2)
                    pink_x3_3_3_mean = pink_x3_3_3.mean()
                    pink_x3_3_3_min_val, pink_x3_3_3_max_val, pink_x3_3_3_min_index, pink_x3_3_3_max_index = cv2.minMaxLoc(pink_x3_3_3)
                    background = cv2.imread("background.png", cv2.IMREAD_COLOR)
                    background_h, background_w, background_c = background.shape
                    if os.path.exists(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\ip.txt'):
                        ip_txt = open(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\ip.txt', "r")
                        background = cv2.putText(background, ip_txt.readline(), (300, 60), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        chip_txt.close()
                    background = cv2.putText(background, time.strftime("%Y-%m-%d %H:%M:%S", time.localtime()), (300, 90), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                    color_gray_x3_mean_min = 20
                    if color_gray_x3_1_1_min_val < color_gray_x3_mean_min or color_gray_x3_1_2_min_val < color_gray_x3_mean_min or color_gray_x3_1_3_min_val < color_gray_x3_mean_min \
                        or color_gray_x3_2_1_min_val < color_gray_x3_mean_min or color_gray_x3_2_2_min_val < color_gray_x3_mean_min or color_gray_x3_2_3_min_val < color_gray_x3_mean_min \
                        or color_gray_x3_3_1_min_val < color_gray_x3_mean_min or color_gray_x3_3_2_min_val < color_gray_x3_mean_min or color_gray_x3_3_3_min_val < color_gray_x3_mean_min:
                        color_x3_1_1 = color_gray_x3_1_1_min_val > color_gray_x3_mean_min
                        color_x3_1_2 = color_gray_x3_1_2_min_val > color_gray_x3_mean_min
                        color_x3_1_3 = color_gray_x3_1_3_min_val > color_gray_x3_mean_min
                        color_x3_2_1 = color_gray_x3_2_1_min_val > color_gray_x3_mean_min
                        color_x3_2_2 = color_gray_x3_2_2_min_val > color_gray_x3_mean_min
                        color_x3_2_3 = color_gray_x3_2_3_min_val > color_gray_x3_mean_min
                        color_x3_3_1 = color_gray_x3_3_1_min_val > color_gray_x3_mean_min
                        color_x3_3_2 = color_gray_x3_3_2_min_val > color_gray_x3_mean_min
                        color_x3_3_3 = color_gray_x3_3_3_min_val > color_gray_x3_mean_min
                        color_pink_x3_1_1 = pink_x3_1_1_max_val > color_gray_x3_mean_min
                        color_pink_x3_1_2 = pink_x3_1_2_max_val > color_gray_x3_mean_min
                        color_pink_x3_1_3 = pink_x3_1_3_max_val > color_gray_x3_mean_min
                        color_pink_x3_2_1 = pink_x3_2_1_max_val > color_gray_x3_mean_min
                        color_pink_x3_2_2 = pink_x3_2_2_max_val > color_gray_x3_mean_min
                        color_pink_x3_2_3 = pink_x3_2_3_max_val > color_gray_x3_mean_min
                        color_pink_x3_3_1 = pink_x3_3_1_max_val > color_gray_x3_mean_min
                        color_pink_x3_3_2 = pink_x3_3_2_max_val > color_gray_x3_mean_min
                        color_pink_x3_3_3 = pink_x3_3_3_max_val > color_gray_x3_mean_min
                        cv2.line(background, (0, 100), (background_w, 100), (0, 0, 0))
                        cv2.line(background, (0, 300), (background_w, 300), (0, 0, 0))
                        background = cv2.putText(background, name, (0, 20), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background = cv2.putText(background, '3X3', (150, 90), cv2.FONT_HERSHEY_DUPLEX, 2, (0, 0, 0))
                        background[100:300, 0:200] = color_x3_200
                        background_x3_mean = background[100:300, 200:500]
                        background_x3_mean_h, background_x3_mean_w, background_x3_mean_c = background_x3_mean.shape
                        cv2.line(background_x3_mean, (100, 0), (100, background_x3_mean_h), (0, 0, 0))
                        cv2.line(background_x3_mean, (200, 0), (200, background_x3_mean_h), (0, 0, 0))
                        cv2.circle(background_x3_mean, (66, 33), 30, (0, 0, 255) if color_gray_x3_1_1_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x3_mean, (166, 33), 30, (0, 0, 255) if color_gray_x3_1_2_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x3_mean, (266, 33), 30, (0, 0, 255) if color_gray_x3_1_3_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x3_mean, (66, 99), 30, (0, 0, 255) if color_gray_x3_2_1_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x3_mean, (166, 99), 30, (0, 0, 255) if color_gray_x3_2_2_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x3_mean, (266, 99), 30, (0, 0, 255) if color_gray_x3_2_3_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x3_mean, (66, 165), 30, (0, 0, 255) if color_gray_x3_3_1_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x3_mean, (166, 165), 30, (0, 0, 255) if color_gray_x3_3_2_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x3_mean, (266, 165), 30, (0, 0, 255) if color_gray_x3_3_3_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        if color_pink_x3_1_1:
                            cv2.circle(background_x3_mean, (66, 33), 30, (0, 255, 255), -1)
                        if color_pink_x3_1_2:
                            cv2.circle(background_x3_mean, (166, 33), 30, (0, 255, 255), -1)
                        if color_pink_x3_1_3:
                            cv2.circle(background_x3_mean, (266, 33), 30, (0, 255, 255), -1)
                        if color_pink_x3_2_1:
                            cv2.circle(background_x3_mean, (66, 99), 30, (0, 255, 255), -1)
                        if color_pink_x3_2_2:
                            cv2.circle(background_x3_mean, (166, 99), 30, (0, 255, 255), -1)
                        if color_pink_x3_2_3:
                            cv2.circle(background_x3_mean, (266, 99), 30, (0, 255, 255), -1)
                        if color_pink_x3_3_1:
                            cv2.circle(background_x3_mean, (66, 165), 30, (0, 255, 255), -1)
                        if color_pink_x3_3_2:
                            cv2.circle(background_x3_mean, (166, 165), 30, (0, 255, 255), -1)
                        if color_pink_x3_3_3:
                            cv2.circle(background_x3_mean, (266, 165), 30, (0, 255, 255), -1)
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_1_1_min_val), (0, 66), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_1_2_min_val), (100, 66), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_1_3_min_val), (200, 66), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_2_1_min_val), (0, 132), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_2_2_min_val), (100, 132), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_2_3_min_val), (200, 132), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_3_1_min_val), (0, 198), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_3_2_min_val), (100, 198), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        background_x3_mean = cv2.putText(background_x3_mean, str(color_gray_x3_3_3_min_val), (200, 198), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                        yolo_2_2 = False
                        yolo_2_2_index = 0
                        for r in yolo_rects:
                            if r[0] < chip_center[0] and r[1] < chip_center[1] and r[0] + r[2] > chip_center[0] and r[1] + r[3] > chip_center[1]:
                                yolo_2_2 = True
                                break
                            yolo_2_2_index += 1
                        yolo_width = 100
                        yolo_height = 100
                        yolo_2_1 = False
                        yolo_2_3 = False
                        yolo_1_2 = False
                        yolo_3_2 = False
                        yolo_1_1 = False
                        yolo_1_3 = False
                        yolo_3_1 = False
                        yolo_3_3 = False
                        yolo_2_1_index = 0
                        yolo_2_3_index = 0
                        yolo_1_2_index = 0
                        yolo_3_2_index = 0
                        yolo_1_1_index = 0
                        yolo_1_3_index = 0
                        yolo_3_1_index = 0
                        yolo_3_3_index = 0
                        if yolo_2_2:
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1]) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and yolo_width > abs(r[0] - yolo_rects[yolo_2_2_index][0]) \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0]) > yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_width = abs(r[0] - yolo_rects[yolo_2_2_index][0])
                                if abs(r[0] - yolo_rects[yolo_2_2_index][0]) < yolo_rects[yolo_2_2_index][2] / 2 \
                                        and yolo_height > abs(r[1] - yolo_rects[yolo_2_2_index][1]) \
                                        and abs(r[1] - yolo_rects[yolo_2_2_index][1]) > yolo_rects[yolo_2_2_index][3] / 2:
                                    yolo_height = abs(r[1] - yolo_rects[yolo_2_2_index][1])
                            if (not color_x3_2_1) and (not color_x3_2_3):
                                yolo_width = yolo_width / 2
                            if (not color_x3_1_2) and (not color_x3_3_2):
                                yolo_height = yolo_height / 2
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1]) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0] - yolo_width) < yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_2_3 = True
                                    break
                                yolo_2_3_index += 1
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1]) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0] + yolo_width) < yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_2_1 = True
                                    break
                                yolo_2_1_index += 1
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1] + yolo_height) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0]) < yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_1_2 = True
                                    break
                                yolo_1_2_index += 1
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1] - yolo_height) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0]) < yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_3_2 = True
                                    break
                                yolo_3_2_index += 1
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1] + yolo_height) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0] + yolo_width) < yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_1_1 = True
                                    break
                                yolo_1_1_index += 1
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1] + yolo_height) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0] - yolo_width) < yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_1_3 = True
                                    break
                                yolo_1_3_index += 1
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1] - yolo_height) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0] + yolo_width) < yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_3_1 = True
                                    break
                                yolo_3_1_index += 1
                            for r in yolo_rects:
                                if abs(r[1] - yolo_rects[yolo_2_2_index][1] - yolo_height) < yolo_rects[yolo_2_2_index][3] / 2 \
                                        and abs(r[0] - yolo_rects[yolo_2_2_index][0] - yolo_width) < yolo_rects[yolo_2_2_index][2] / 2:
                                    yolo_3_3 = True
                                    break
                                yolo_3_3_index += 1
                            chip_x3 = chip[int(yolo_rects[yolo_2_2_index][1] - yolo_height) : int(yolo_rects[yolo_2_2_index][1] + yolo_height * 2), int(yolo_rects[yolo_2_2_index][0] - yolo_width) : int(yolo_rects[yolo_2_2_index][0] + yolo_width * 2)]
                            chip_x3_h, chip_x3_w, chip_x3_c = chip_x3.shape
                            chip_x3_200_size = (200, 200)
                            if chip_x3_w < chip_x3_h:
                                chip_x3_200_size = (int(200 * chip_x3_w / chip_x3_h), 200)
                            else:
                                chip_x3_200_size = (200, int(200 * chip_x3_h / chip_x3_w))
                            chip_x3_200 = cv2.resize(chip_x3, chip_x3_200_size)
                            background[300:300+chip_x3_200_size[1], 0:chip_x3_200_size[0]] = chip_x3_200
                            background_x3_mean = background[300:500,200:500]
                            background_x3_mean_h, background_x3_mean_w, background_x3_mean_c = background_x3_mean.shape
                            cv2.line(background_x3_mean, (100, 0), (100, background_x3_mean_h), (0, 0, 0))
                            cv2.line(background_x3_mean, (200, 0), (200, background_x3_mean_h), (0, 0, 0))
                            cv2.circle(background_x3_mean, (66, 33), 30, (0, 255, 0) if yolo_1_1 else (0, 0, 255), -1)
                            cv2.circle(background_x3_mean, (166, 33), 30, (0, 255, 0) if yolo_1_2 else (0, 0, 255), -1)
                            cv2.circle(background_x3_mean, (266, 33), 30, (0, 255, 0) if yolo_1_3 else (0, 0, 255), -1)
                            cv2.circle(background_x3_mean, (66, 99), 30, (0, 255, 0) if yolo_2_1 else (0, 0, 255), -1)
                            cv2.circle(background_x3_mean, (166, 99), 30, (0, 255, 0) if yolo_2_2 else (0, 0, 255), -1)
                            cv2.circle(background_x3_mean, (266, 99), 30, (0, 255, 0) if yolo_2_3 else (0, 0, 255), -1)
                            cv2.circle(background_x3_mean, (66, 165), 30, (0, 255, 0) if yolo_3_1 else (0, 0, 255), -1)
                            cv2.circle(background_x3_mean, (166, 165), 30, (0, 255, 0) if yolo_3_2 else (0, 0, 255), -1)
                            cv2.circle(background_x3_mean, (266, 165), 30, (0, 255, 0) if yolo_3_3 else (0, 0, 255), -1)
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_1_1_index])) if yolo_1_1 else '0', (0, 66), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_1_2_index])) if yolo_1_2 else '0', (100, 66), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_1_3_index])) if yolo_1_3 else '0', (200, 66), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_2_1_index])) if yolo_2_1 else '0', (0, 132), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_2_2_index])) if yolo_2_2 else '0', (100, 132), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_2_3_index])) if yolo_2_3 else '0', (200, 132), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_3_1_index])) if yolo_3_1 else '0', (0, 198), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_3_2_index])) if yolo_3_2 else '0', (100, 198), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            background_x3_mean = cv2.putText(background_x3_mean, "{:.2f}".format(float(yolo_scores[yolo_3_3_index])) if yolo_3_3 else '0', (200, 198), cv2.FONT_HERSHEY_DUPLEX, 1, (0, 0, 0))
                            if color_pink_x3_1_1:
                                color_x3_1_1 = yolo_1_1
                            if color_pink_x3_1_2:
                                color_x3_1_2 = yolo_1_2
                            if color_pink_x3_1_3:
                                color_x3_1_3 = yolo_1_3
                            if color_pink_x3_2_1:
                                color_x3_2_1 = yolo_2_1
                            if color_pink_x3_2_2:
                                color_x3_2_2 = yolo_2_2
                            if color_pink_x3_2_3:
                                color_x3_2_3 = yolo_2_3
                            if color_pink_x3_3_1:
                                color_x3_3_1 = yolo_3_1
                            if color_pink_x3_3_2:
                                color_x3_3_2 = yolo_3_2
                            if color_pink_x3_3_3:
                                color_x3_3_3 = yolo_3_3
                            if color_x3_1_1 == yolo_1_1 and color_x3_1_2 == yolo_1_2 and color_x3_1_3 == yolo_1_3 \
                                    and color_x3_2_1 == yolo_2_1 and color_x3_2_2 == yolo_2_2 and color_x3_2_3 == yolo_2_3 \
                                    and color_x3_3_1 == yolo_3_1 and color_x3_3_2 == yolo_3_2 and color_x3_3_3 == yolo_3_3:
                                background = cv2.putText(background, 'OK', (0, 95), cv2.FONT_HERSHEY_DUPLEX, 3, (0, 255, 0), 3)
                                result.write("OK\n")
                                result.write("图档符合\n")
                                cursor.execute(sql_update1 + ' res=\'OK\', res_ch=\'图档符合\', x=\'3X3\' ' \
                                               + sql_update2 + '\'' + image.replace('\\', '\\\\') + '\'')
                                conn.commit()
                            else:
                                background = cv2.putText(background, 'NG', (0, 95), cv2.FONT_HERSHEY_DUPLEX, 3, (0, 0, 255), 3)
                                result.write("NG\n")
                                result.write("图档不符\n")
                                cursor.execute(sql_update1 + ' res=\'NG\', res_ch=\'图档不符\', x=\'3X3\' ' \
                                               + sql_update2 + '\'' + image.replace('\\', '\\\\') + '\'')
                                conn.commit()
                        else:
                            chip_x3_200 = chip[chip_center[1]-100:chip_center[1]+100, chip_center[0]-100:chip_center[0]+100]
                            background[300:500, 0:200] = chip_x3_200
                            background = cv2.putText(background, 'RE', (0, 95), cv2.FONT_HERSHEY_DUPLEX, 3, (255, 0, 0), 3)
                            result.write("RE\n")
                            result.write("复判,找不到中心芯粒\n")
                            cursor.execute(sql_update1 + ' res=\'RE\', res_ch=\'复判,找不到中心芯粒\', x=\'3X3\' ' \
                                           + sql_update2 + '\'' + image.replace('\\', '\\\\') + '\'')
                            conn.commit()
                        cv2.imwrite(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\result.jpg', background)
                        result.write("3X3\n")
                        result.flush()
                        result.close()
                    else:
                        color_x5 = color[rect_min[1] - 12:rect_min[1] + 13, rect_min[0] - 12:rect_min[0] + 13]
                        color_x5_200 = cv2.resize(color_x5, (200, 200))
                        color_gray_x5_1_1 = color_gray[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] - 11:rect_min[0] - 8]
                        color_gray_x5_1_2 = color_gray[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] - 6:rect_min[0] - 3]
                        color_gray_x5_1_3 = color_gray[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] - 1:rect_min[0] + 2]
                        color_gray_x5_1_4 = color_gray[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] + 4:rect_min[0] + 7]
                        color_gray_x5_1_5 = color_gray[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] + 9:rect_min[0] + 12]
                        color_gray_x5_2_1 = color_gray[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] - 11:rect_min[0] - 8]
                        color_gray_x5_2_2 = color_gray[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] - 6:rect_min[0] - 3]
                        color_gray_x5_2_3 = color_gray[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] - 1:rect_min[0] + 2]
                        color_gray_x5_2_4 = color_gray[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] + 4:rect_min[0] + 7]
                        color_gray_x5_2_5 = color_gray[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] + 9:rect_min[0] + 12]
                        color_gray_x5_3_1 = color_gray[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] - 11:rect_min[0] - 8]
                        color_gray_x5_3_2 = color_gray[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] - 6:rect_min[0] - 3]
                        color_gray_x5_3_3 = color_gray[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] - 1:rect_min[0] + 2]
                        color_gray_x5_3_4 = color_gray[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] + 4:rect_min[0] + 7]
                        color_gray_x5_3_5 = color_gray[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] + 9:rect_min[0] + 12]
                        color_gray_x5_4_1 = color_gray[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] - 11:rect_min[0] - 8]
                        color_gray_x5_4_2 = color_gray[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] - 6:rect_min[0] - 3]
                        color_gray_x5_4_3 = color_gray[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] - 1:rect_min[0] + 2]
                        color_gray_x5_4_4 = color_gray[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] + 4:rect_min[0] + 7]
                        color_gray_x5_4_5 = color_gray[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] + 9:rect_min[0] + 12]
                        color_gray_x5_5_1 = color_gray[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] - 11:rect_min[0] - 8]
                        color_gray_x5_5_2 = color_gray[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] - 6:rect_min[0] - 3]
                        color_gray_x5_5_3 = color_gray[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] - 1:rect_min[0] + 2]
                        color_gray_x5_5_4 = color_gray[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] + 4:rect_min[0] + 7]
                        color_gray_x5_5_5 = color_gray[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] + 9:rect_min[0] + 12]
                        color_gray_x5_1_1_mean = color_gray_x5_1_1.mean()
                        color_gray_x5_1_1_min_val, color_gray_x5_1_1_max_val, color_gray_x5_1_1_min_index, color_gray_x5_1_1_max_index = cv2.minMaxLoc(color_gray_x5_1_1)
                        color_gray_x5_1_2_mean = color_gray_x5_1_2.mean()
                        color_gray_x5_1_2_min_val, color_gray_x5_1_2_max_val, color_gray_x5_1_2_min_index, color_gray_x5_1_2_max_index = cv2.minMaxLoc(color_gray_x5_1_2)
                        color_gray_x5_1_3_mean = color_gray_x5_1_3.mean()
                        color_gray_x5_1_3_min_val, color_gray_x5_1_3_max_val, color_gray_x5_1_3_min_index, color_gray_x5_1_3_max_index = cv2.minMaxLoc(color_gray_x5_1_3)
                        color_gray_x5_1_4_mean = color_gray_x5_1_4.mean()
                        color_gray_x5_1_4_min_val, color_gray_x5_1_4_max_val, color_gray_x5_1_4_min_index, color_gray_x5_1_4_max_index = cv2.minMaxLoc(color_gray_x5_1_4)
                        color_gray_x5_1_5_mean = color_gray_x5_1_5.mean()
                        color_gray_x5_1_5_min_val, color_gray_x5_1_5_max_val, color_gray_x5_1_5_min_index, color_gray_x5_1_5_max_index = cv2.minMaxLoc(color_gray_x5_1_5)
                        color_gray_x5_2_1_mean = color_gray_x5_2_1.mean()
                        color_gray_x5_2_1_min_val, color_gray_x5_2_1_max_val, color_gray_x5_2_1_min_index, color_gray_x5_2_1_max_index = cv2.minMaxLoc(color_gray_x5_2_1)
                        color_gray_x5_2_2_mean = color_gray_x5_2_2.mean()
                        color_gray_x5_2_2_min_val, color_gray_x5_2_2_max_val, color_gray_x5_2_2_min_index, color_gray_x5_2_2_max_index = cv2.minMaxLoc(color_gray_x5_2_2)
                        color_gray_x5_2_3_mean = color_gray_x5_2_3.mean()
                        color_gray_x5_2_3_min_val, color_gray_x5_2_3_max_val, color_gray_x5_2_3_min_index, color_gray_x5_2_3_max_index = cv2.minMaxLoc(color_gray_x5_2_3)
                        color_gray_x5_2_4_mean = color_gray_x5_2_4.mean()
                        color_gray_x5_2_4_min_val, color_gray_x5_2_4_max_val, color_gray_x5_2_4_min_index, color_gray_x5_2_4_max_index = cv2.minMaxLoc(color_gray_x5_2_4)
                        color_gray_x5_2_5_mean = color_gray_x5_2_5.mean()
                        color_gray_x5_2_5_min_val, color_gray_x5_2_5_max_val, color_gray_x5_2_5_min_index, color_gray_x5_2_5_max_index = cv2.minMaxLoc(color_gray_x5_2_5)
                        color_gray_x5_3_1_mean = color_gray_x5_3_1.mean()
                        color_gray_x5_3_1_min_val, color_gray_x5_3_1_max_val, color_gray_x5_3_1_min_index, color_gray_x5_3_1_max_index = cv2.minMaxLoc(color_gray_x5_3_1)
                        color_gray_x5_3_2_mean = color_gray_x5_3_2.mean()
                        color_gray_x5_3_2_min_val, color_gray_x5_3_2_max_val, color_gray_x5_3_2_min_index, color_gray_x5_3_2_max_index = cv2.minMaxLoc(color_gray_x5_3_2)
                        color_gray_x5_3_3_mean = color_gray_x5_3_3.mean()
                        color_gray_x5_3_3_min_val, color_gray_x5_3_3_max_val, color_gray_x5_3_3_min_index, color_gray_x5_3_3_max_index = cv2.minMaxLoc(color_gray_x5_3_3)
                        color_gray_x5_3_4_mean = color_gray_x5_3_4.mean()
                        color_gray_x5_3_4_min_val, color_gray_x5_3_4_max_val, color_gray_x5_3_4_min_index, color_gray_x5_3_4_max_index = cv2.minMaxLoc(color_gray_x5_3_4)
                        color_gray_x5_3_5_mean = color_gray_x5_3_5.mean()
                        color_gray_x5_3_5_min_val, color_gray_x5_3_5_max_val, color_gray_x5_3_5_min_index, color_gray_x5_3_5_max_index = cv2.minMaxLoc(color_gray_x5_3_5)
                        color_gray_x5_4_1_mean = color_gray_x5_4_1.mean()
                        color_gray_x5_4_1_min_val, color_gray_x5_4_1_max_val, color_gray_x5_4_1_min_index, color_gray_x5_4_1_max_index = cv2.minMaxLoc(color_gray_x5_4_1)
                        color_gray_x5_4_2_mean = color_gray_x5_4_2.mean()
                        color_gray_x5_4_2_min_val, color_gray_x5_4_2_max_val, color_gray_x5_4_2_min_index, color_gray_x5_4_2_max_index = cv2.minMaxLoc(color_gray_x5_4_2)
                        color_gray_x5_4_3_mean = color_gray_x5_4_3.mean()
                        color_gray_x5_4_3_min_val, color_gray_x5_4_3_max_val, color_gray_x5_4_3_min_index, color_gray_x5_4_3_max_index = cv2.minMaxLoc(color_gray_x5_4_3)
                        color_gray_x5_4_4_mean = color_gray_x5_4_4.mean()
                        color_gray_x5_4_4_min_val, color_gray_x5_4_4_max_val, color_gray_x5_4_4_min_index, color_gray_x5_4_4_max_index = cv2.minMaxLoc(color_gray_x5_4_4)
                        color_gray_x5_4_5_mean = color_gray_x5_4_5.mean()
                        color_gray_x5_4_5_min_val, color_gray_x5_4_5_max_val, color_gray_x5_4_5_min_index, color_gray_x5_4_5_max_index = cv2.minMaxLoc(color_gray_x5_4_5)
                        color_gray_x5_5_1_mean = color_gray_x5_5_1.mean()
                        color_gray_x5_5_1_min_val, color_gray_x5_5_1_max_val, color_gray_x5_5_1_min_index, color_gray_x5_5_1_max_index = cv2.minMaxLoc(color_gray_x5_5_1)
                        color_gray_x5_5_2_mean = color_gray_x5_5_2.mean()
                        color_gray_x5_5_2_min_val, color_gray_x5_5_2_max_val, color_gray_x5_5_2_min_index, color_gray_x5_5_2_max_index = cv2.minMaxLoc(color_gray_x5_5_2)
                        color_gray_x5_5_3_mean = color_gray_x5_5_3.mean()
                        color_gray_x5_5_3_min_val, color_gray_x5_5_3_max_val, color_gray_x5_5_3_min_index, color_gray_x5_5_3_max_index = cv2.minMaxLoc(color_gray_x5_5_3)
                        color_gray_x5_5_4_mean = color_gray_x5_5_4.mean()
                        color_gray_x5_5_4_min_val, color_gray_x5_5_4_max_val, color_gray_x5_5_4_min_index, color_gray_x5_5_4_max_index = cv2.minMaxLoc(color_gray_x5_5_4)
                        color_gray_x5_5_5_mean = color_gray_x5_5_5.mean()
                        color_gray_x5_5_5_min_val, color_gray_x5_5_5_max_val, color_gray_x5_5_5_min_index, color_gray_x5_5_5_max_index = cv2.minMaxLoc(color_gray_x5_5_5)
                        pink_x5_1_1 = pink[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] - 11:rect_min[0] - 8]
                        pink_x5_1_2 = pink[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] - 6:rect_min[0] - 3]
                        pink_x5_1_3 = pink[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] - 1:rect_min[0] + 2]
                        pink_x5_1_4 = pink[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] + 4:rect_min[0] + 7]
                        pink_x5_1_5 = pink[rect_min[1] - 11:rect_min[1] - 8, rect_min[0] + 9:rect_min[0] + 12]
                        pink_x5_2_1 = pink[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] - 11:rect_min[0] - 8]
                        pink_x5_2_2 = pink[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] - 6:rect_min[0] - 3]
                        pink_x5_2_3 = pink[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] - 1:rect_min[0] + 2]
                        pink_x5_2_4 = pink[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] + 4:rect_min[0] + 7]
                        pink_x5_2_5 = pink[rect_min[1] - 6:rect_min[1] - 3, rect_min[0] + 9:rect_min[0] + 12]
                        pink_x5_3_1 = pink[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] - 11:rect_min[0] - 8]
                        pink_x5_3_2 = pink[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] - 6:rect_min[0] - 3]
                        pink_x5_3_3 = pink[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] - 1:rect_min[0] + 2]
                        pink_x5_3_4 = pink[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] + 4:rect_min[0] + 7]
                        pink_x5_3_5 = pink[rect_min[1] - 1:rect_min[1] + 2, rect_min[0] + 9:rect_min[0] + 12]
                        pink_x5_4_1 = pink[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] - 11:rect_min[0] - 8]
                        pink_x5_4_2 = pink[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] - 6:rect_min[0] - 3]
                        pink_x5_4_3 = pink[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] - 1:rect_min[0] + 2]
                        pink_x5_4_4 = pink[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] + 4:rect_min[0] + 7]
                        pink_x5_4_5 = pink[rect_min[1] + 4:rect_min[1] + 7, rect_min[0] + 9:rect_min[0] + 12]
                        pink_x5_5_1 = pink[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] - 11:rect_min[0] - 8]
                        pink_x5_5_2 = pink[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] - 6:rect_min[0] - 3]
                        pink_x5_5_3 = pink[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] - 1:rect_min[0] + 2]
                        pink_x5_5_4 = pink[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] + 4:rect_min[0] + 7]
                        pink_x5_5_5 = pink[rect_min[1] + 9:rect_min[1] + 12, rect_min[0] + 9:rect_min[0] + 12]
                        pink_x5_1_1_mean = pink_x5_1_1.mean()
                        pink_x5_1_1_min_val, pink_x5_1_1_max_val, pink_x5_1_1_min_index, pink_x5_1_1_max_index = cv2.minMaxLoc(pink_x5_1_1)
                        pink_x5_1_2_mean = pink_x5_1_2.mean()
                        pink_x5_1_2_min_val, pink_x5_1_2_max_val, pink_x5_1_2_min_index, pink_x5_1_2_max_index = cv2.minMaxLoc(pink_x5_1_2)
                        pink_x5_1_3_mean = pink_x5_1_3.mean()
                        pink_x5_1_3_min_val, pink_x5_1_3_max_val, pink_x5_1_3_min_index, pink_x5_1_3_max_index = cv2.minMaxLoc(pink_x5_1_3)
                        pink_x5_1_4_mean = pink_x5_1_4.mean()
                        pink_x5_1_4_min_val, pink_x5_1_4_max_val, pink_x5_1_4_min_index, pink_x5_1_4_max_index = cv2.minMaxLoc(pink_x5_1_4)
                        pink_x5_1_5_mean = pink_x5_1_5.mean()
                        pink_x5_1_5_min_val, pink_x5_1_5_max_val, pink_x5_1_5_min_index, pink_x5_1_5_max_index = cv2.minMaxLoc(pink_x5_1_5)
                        pink_x5_2_1_mean = pink_x5_2_1.mean()
                        pink_x5_2_1_min_val, pink_x5_2_1_max_val, pink_x5_2_1_min_index, pink_x5_2_1_max_index = cv2.minMaxLoc(pink_x5_2_1)
                        pink_x5_2_2_mean = pink_x5_2_2.mean()
                        pink_x5_2_2_min_val, pink_x5_2_2_max_val, pink_x5_2_2_min_index, pink_x5_2_2_max_index = cv2.minMaxLoc(pink_x5_2_2)
                        pink_x5_2_3_mean = pink_x5_2_3.mean()
                        pink_x5_2_3_min_val, pink_x5_2_3_max_val, pink_x5_2_3_min_index, pink_x5_2_3_max_index = cv2.minMaxLoc(pink_x5_2_3)
                        pink_x5_2_4_mean = pink_x5_2_4.mean()
                        pink_x5_2_4_min_val, pink_x5_2_4_max_val, pink_x5_2_4_min_index, pink_x5_2_4_max_index = cv2.minMaxLoc(pink_x5_2_4)
                        pink_x5_2_5_mean = pink_x5_2_5.mean()
                        pink_x5_2_5_min_val, pink_x5_2_5_max_val, pink_x5_2_5_min_index, pink_x5_2_5_max_index = cv2.minMaxLoc(pink_x5_2_5)
                        pink_x5_3_1_mean = pink_x5_3_1.mean()
                        pink_x5_3_1_min_val, pink_x5_3_1_max_val, pink_x5_3_1_min_index, pink_x5_3_1_max_index = cv2.minMaxLoc(pink_x5_3_1)
                        pink_x5_3_2_mean = pink_x5_3_2.mean()
                        pink_x5_3_2_min_val, pink_x5_3_2_max_val, pink_x5_3_2_min_index, pink_x5_3_2_max_index = cv2.minMaxLoc(pink_x5_3_2)
                        pink_x5_3_3_mean = pink_x5_3_3.mean()
                        pink_x5_3_3_min_val, pink_x5_3_3_max_val, pink_x5_3_3_min_index, pink_x5_3_3_max_index = cv2.minMaxLoc(pink_x5_3_3)
                        pink_x5_3_4_mean = pink_x5_3_4.mean()
                        pink_x5_3_4_min_val, pink_x5_3_4_max_val, pink_x5_3_4_min_index, pink_x5_3_4_max_index = cv2.minMaxLoc(pink_x5_3_4)
                        pink_x5_3_5_mean = pink_x5_3_5.mean()
                        pink_x5_3_5_min_val, pink_x5_3_5_max_val, pink_x5_3_5_min_index, pink_x5_3_5_max_index = cv2.minMaxLoc(pink_x5_3_5)
                        pink_x5_4_1_mean = pink_x5_4_1.mean()
                        pink_x5_4_1_min_val, pink_x5_4_1_max_val, pink_x5_4_1_min_index, pink_x5_4_1_max_index = cv2.minMaxLoc(pink_x5_4_1)
                        pink_x5_4_2_mean = pink_x5_4_2.mean()
                        pink_x5_4_2_min_val, pink_x5_4_2_max_val, pink_x5_4_2_min_index, pink_x5_4_2_max_index = cv2.minMaxLoc(pink_x5_4_2)
                        pink_x5_4_3_mean = pink_x5_4_3.mean()
                        pink_x5_4_3_min_val, pink_x5_4_3_max_val, pink_x5_4_3_min_index, pink_x5_4_3_max_index = cv2.minMaxLoc(pink_x5_4_3)
                        pink_x5_4_4_mean = pink_x5_4_4.mean()
                        pink_x5_4_4_min_val, pink_x5_4_4_max_val, pink_x5_4_4_min_index, pink_x5_4_4_max_index = cv2.minMaxLoc(pink_x5_4_4)
                        pink_x5_4_5_mean = pink_x5_4_5.mean()
                        pink_x5_4_5_min_val, pink_x5_4_5_max_val, pink_x5_4_5_min_index, pink_x5_4_5_max_index = cv2.minMaxLoc(pink_x5_4_5)
                        pink_x5_5_1_mean = pink_x5_5_1.mean()
                        pink_x5_5_1_min_val, pink_x5_5_1_max_val, pink_x5_5_1_min_index, pink_x5_5_1_max_index = cv2.minMaxLoc(pink_x5_5_1)
                        pink_x5_5_2_mean = pink_x5_5_2.mean()
                        pink_x5_5_2_min_val, pink_x5_5_2_max_val, pink_x5_5_2_min_index, pink_x5_5_2_max_index = cv2.minMaxLoc(pink_x5_5_2)
                        pink_x5_5_3_mean = pink_x5_5_3.mean()
                        pink_x5_5_3_min_val, pink_x5_5_3_max_val, pink_x5_5_3_min_index, pink_x5_5_3_max_index = cv2.minMaxLoc(pink_x5_5_3)
                        pink_x5_5_4_mean = pink_x5_5_4.mean()
                        pink_x5_5_4_min_val, pink_x5_5_4_max_val, pink_x5_5_4_min_index, pink_x5_5_4_max_index = cv2.minMaxLoc(pink_x5_5_4)
                        pink_x5_5_5_mean = pink_x5_5_5.mean()
                        pink_x5_5_5_min_val, pink_x5_5_5_max_val, pink_x5_5_5_min_index, pink_x5_5_5_max_index = cv2.minMaxLoc(pink_x5_5_5)
                        color_x5_1_1 = color_gray_x5_1_1_min_val > color_gray_x3_mean_min
                        color_x5_1_2 = color_gray_x5_1_2_min_val > color_gray_x3_mean_min
                        color_x5_1_3 = color_gray_x5_1_3_min_val > color_gray_x3_mean_min
                        color_x5_1_4 = color_gray_x5_1_4_min_val > color_gray_x3_mean_min
                        color_x5_1_5 = color_gray_x5_1_5_min_val > color_gray_x3_mean_min
                        color_x5_2_1 = color_gray_x5_2_1_min_val > color_gray_x3_mean_min
                        color_x5_2_2 = color_gray_x5_2_2_min_val > color_gray_x3_mean_min
                        color_x5_2_3 = color_gray_x5_2_3_min_val > color_gray_x3_mean_min
                        color_x5_2_4 = color_gray_x5_2_4_min_val > color_gray_x3_mean_min
                        color_x5_2_5 = color_gray_x5_2_5_min_val > color_gray_x3_mean_min
                        color_x5_3_1 = color_gray_x5_3_1_min_val > color_gray_x3_mean_min
                        color_x5_3_2 = color_gray_x5_3_2_min_val > color_gray_x3_mean_min
                        color_x5_3_3 = color_gray_x5_3_3_min_val > color_gray_x3_mean_min
                        color_x5_3_4 = color_gray_x5_3_4_min_val > color_gray_x3_mean_min
                        color_x5_3_5 = color_gray_x5_3_5_min_val > color_gray_x3_mean_min
                        color_x5_4_1 = color_gray_x5_4_1_min_val > color_gray_x3_mean_min
                        color_x5_4_2 = color_gray_x5_4_2_min_val > color_gray_x3_mean_min
                        color_x5_4_3 = color_gray_x5_4_3_min_val > color_gray_x3_mean_min
                        color_x5_4_4 = color_gray_x5_4_4_min_val > color_gray_x3_mean_min
                        color_x5_4_5 = color_gray_x5_4_5_min_val > color_gray_x3_mean_min
                        color_x5_5_1 = color_gray_x5_5_1_min_val > color_gray_x3_mean_min
                        color_x5_5_2 = color_gray_x5_5_2_min_val > color_gray_x3_mean_min
                        color_x5_5_3 = color_gray_x5_5_3_min_val > color_gray_x3_mean_min
                        color_x5_5_4 = color_gray_x5_5_4_min_val > color_gray_x3_mean_min
                        color_x5_5_5 = color_gray_x5_5_5_min_val > color_gray_x3_mean_min
                        color_pink_x5_1_1 = pink_x5_1_1_max_val > color_gray_x3_mean_min
                        color_pink_x5_1_2 = pink_x5_1_2_max_val > color_gray_x3_mean_min
                        color_pink_x5_1_3 = pink_x5_1_3_max_val > color_gray_x3_mean_min
                        color_pink_x5_1_4 = pink_x5_1_4_max_val > color_gray_x3_mean_min
                        color_pink_x5_1_5 = pink_x5_1_5_max_val > color_gray_x3_mean_min
                        color_pink_x5_2_1 = pink_x5_2_1_max_val > color_gray_x3_mean_min
                        color_pink_x5_2_2 = pink_x5_2_2_max_val > color_gray_x3_mean_min
                        color_pink_x5_2_3 = pink_x5_2_3_max_val > color_gray_x3_mean_min
                        color_pink_x5_2_4 = pink_x5_2_4_max_val > color_gray_x3_mean_min
                        color_pink_x5_2_5 = pink_x5_2_5_max_val > color_gray_x3_mean_min
                        color_pink_x5_3_1 = pink_x5_3_1_max_val > color_gray_x3_mean_min
                        color_pink_x5_3_2 = pink_x5_3_2_max_val > color_gray_x3_mean_min
                        color_pink_x5_3_3 = pink_x5_3_3_max_val > color_gray_x3_mean_min
                        color_pink_x5_3_4 = pink_x5_3_4_max_val > color_gray_x3_mean_min
                        color_pink_x5_3_5 = pink_x5_3_5_max_val > color_gray_x3_mean_min
                        color_pink_x5_4_1 = pink_x5_4_1_max_val > color_gray_x3_mean_min
                        color_pink_x5_4_2 = pink_x5_4_2_max_val > color_gray_x3_mean_min
                        color_pink_x5_4_3 = pink_x5_4_3_max_val > color_gray_x3_mean_min
                        color_pink_x5_4_4 = pink_x5_4_4_max_val > color_gray_x3_mean_min
                        color_pink_x5_4_5 = pink_x5_4_5_max_val > color_gray_x3_mean_min
                        color_pink_x5_5_1 = pink_x5_5_1_max_val > color_gray_x3_mean_min
                        color_pink_x5_5_2 = pink_x5_5_2_max_val > color_gray_x3_mean_min
                        color_pink_x5_5_3 = pink_x5_5_3_max_val > color_gray_x3_mean_min
                        color_pink_x5_5_4 = pink_x5_5_4_max_val > color_gray_x3_mean_min
                        color_pink_x5_5_5 = pink_x5_5_5_max_val > color_gray_x3_mean_min
                        cv2.line(background, (0, 100), (background_w, 100), (0, 0, 0))
                        cv2.line(background, (0, 300), (background_w, 300), (0, 0, 0))
                        background = cv2.putText(background, name, (0, 20), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background = cv2.putText(background, '5X5', (150, 90), cv2.FONT_HERSHEY_DUPLEX, 2, (0, 0, 0))
                        background[100:300, 0:200] = color_x5_200
                        background_x5_mean = background[100:300, 200:500]
                        background_x5_mean_h, background_x5_mean_w, background_x5_mean_c = background_x5_mean.shape
                        cv2.line(background_x5_mean, (60, 0), (60, background_x5_mean_h), (0, 0, 0))
                        cv2.line(background_x5_mean, (120, 0), (120, background_x5_mean_h), (0, 0, 0))
                        cv2.line(background_x5_mean, (180, 0), (180, background_x5_mean_h), (0, 0, 0))
                        cv2.line(background_x5_mean, (240, 0), (240, background_x5_mean_h), (0, 0, 0))
                        cv2.circle(background_x5_mean, (40, 20), 20, (0, 0, 255) if color_gray_x5_1_1_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (100, 20), 20, (0, 0, 255) if color_gray_x5_1_2_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (160, 20), 20, (0, 0, 255) if color_gray_x5_1_3_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (220, 20), 20, (0, 0, 255) if color_gray_x5_1_4_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (280, 20), 20, (0, 0, 255) if color_gray_x5_1_5_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (40, 60), 20, (0, 0, 255) if color_gray_x5_2_1_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (100, 60), 20, (0, 0, 255) if color_gray_x5_2_2_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (160, 60), 20, (0, 0, 255) if color_gray_x5_2_3_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (220, 60), 20, (0, 0, 255) if color_gray_x5_2_4_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (280, 60), 20, (0, 0, 255) if color_gray_x5_2_5_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (40, 100), 20, (0, 0, 255) if color_gray_x5_3_1_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (100, 100), 20, (0, 0, 255) if color_gray_x5_3_2_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (160, 100), 20, (0, 0, 255) if color_gray_x5_3_3_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (220, 100), 20, (0, 0, 255) if color_gray_x5_3_4_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (280, 100), 20, (0, 0, 255) if color_gray_x5_3_5_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (40, 140), 20, (0, 0, 255) if color_gray_x5_4_1_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (100, 140), 20, (0, 0, 255) if color_gray_x5_4_2_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (160, 140), 20, (0, 0, 255) if color_gray_x5_4_3_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (220, 140), 20, (0, 0, 255) if color_gray_x5_4_4_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (280, 140), 20, (0, 0, 255) if color_gray_x5_4_5_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (40, 180), 20, (0, 0, 255) if color_gray_x5_5_1_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (100, 180), 20, (0, 0, 255) if color_gray_x5_5_2_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (160, 180), 20, (0, 0, 255) if color_gray_x5_5_3_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (220, 180), 20, (0, 0, 255) if color_gray_x5_5_4_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        cv2.circle(background_x5_mean, (280, 180), 20, (0, 0, 255) if color_gray_x5_5_5_min_val < color_gray_x3_mean_min else (0, 255, 0), -1)
                        if color_pink_x5_1_1:
                            cv2.circle(background_x5_mean, (40, 20), 20, (0, 255, 255), -1)
                        if color_pink_x5_1_2:
                            cv2.circle(background_x5_mean, (100, 20), 20, (0, 255, 255), -1)
                        if color_pink_x5_1_3:
                            cv2.circle(background_x5_mean, (160, 20), 20, (0, 255, 255), -1)
                        if color_pink_x5_1_4:
                            cv2.circle(background_x5_mean, (220, 20), 20, (0, 255, 255), -1)
                        if color_pink_x5_1_5:
                            cv2.circle(background_x5_mean, (280, 20), 20, (0, 255, 255), -1)
                        if color_pink_x5_2_1:
                            cv2.circle(background_x5_mean, (40, 60), 20, (0, 255, 255), -1)
                        if color_pink_x5_2_2:
                            cv2.circle(background_x5_mean, (100, 60), 20, (0, 255, 255), -1)
                        if color_pink_x5_2_3:
                            cv2.circle(background_x5_mean, (160, 60), 20, (0, 255, 255), -1)
                        if color_pink_x5_2_4:
                            cv2.circle(background_x5_mean, (220, 60), 20, (0, 255, 255), -1)
                        if color_pink_x5_2_5:
                            cv2.circle(background_x5_mean, (280, 60), 20, (0, 255, 255), -1)
                        if color_pink_x5_3_1:
                            cv2.circle(background_x5_mean, (40, 100), 20, (0, 255, 255), -1)
                        if color_pink_x5_3_2:
                            cv2.circle(background_x5_mean, (100, 100), 20, (0, 255, 255), -1)
                        if color_pink_x5_3_3:
                            cv2.circle(background_x5_mean, (160, 100), 20, (0, 255, 255), -1)
                        if color_pink_x5_3_4:
                            cv2.circle(background_x5_mean, (220, 100), 20, (0, 255, 255), -1)
                        if color_pink_x5_3_5:
                            cv2.circle(background_x5_mean, (280, 100), 20, (0, 255, 255), -1)
                        if color_pink_x5_4_1:
                            cv2.circle(background_x5_mean, (40, 140), 20, (0, 255, 255), -1)
                        if color_pink_x5_4_2:
                            cv2.circle(background_x5_mean, (100, 140), 20, (0, 255, 255), -1)
                        if color_pink_x5_4_3:
                            cv2.circle(background_x5_mean, (160, 140), 20, (0, 255, 255), -1)
                        if color_pink_x5_4_4:
                            cv2.circle(background_x5_mean, (220, 140), 20, (0, 255, 255), -1)
                        if color_pink_x5_4_5:
                            cv2.circle(background_x5_mean, (280, 140), 20, (0, 255, 255), -1)
                        if color_pink_x5_5_1:
                            cv2.circle(background_x5_mean, (40, 180), 20, (0, 255, 255), -1)
                        if color_pink_x5_5_2:
                            cv2.circle(background_x5_mean, (100, 180), 20, (0, 255, 255), -1)
                        if color_pink_x5_5_3:
                            cv2.circle(background_x5_mean, (160, 180), 20, (0, 255, 255), -1)
                        if color_pink_x5_5_4:
                            cv2.circle(background_x5_mean, (220, 180), 20, (0, 255, 255), -1)
                        if color_pink_x5_5_5:
                            cv2.circle(background_x5_mean, (280, 180), 20, (0, 255, 255), -1)
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_1_1_min_val), (0, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_1_2_min_val), (60, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_1_3_min_val), (120, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_1_4_min_val), (180, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_1_5_min_val), (240, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_2_1_min_val), (0, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_2_2_min_val), (60, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_2_3_min_val), (120, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_2_4_min_val), (180, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_2_5_min_val), (240, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_3_1_min_val), (0, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_3_2_min_val), (60, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_3_3_min_val), (120, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_3_4_min_val), (180, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_3_5_min_val), (240, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_4_1_min_val), (0, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_4_2_min_val), (60, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_4_3_min_val), (120, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_4_4_min_val), (180, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_4_5_min_val), (240, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_5_1_min_val), (0, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_5_2_min_val), (60, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_5_3_min_val), (120, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_5_4_min_val), (180, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        background_x5_mean = cv2.putText(background_x5_mean, str(color_gray_x5_5_5_min_val), (240, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                        if color_gray_x5_1_1_min_val < color_gray_x3_mean_min or color_gray_x5_1_2_min_val < color_gray_x3_mean_min or color_gray_x5_1_3_min_val < color_gray_x3_mean_min or color_gray_x5_1_4_min_val < color_gray_x3_mean_min or color_gray_x5_1_5_min_val < color_gray_x3_mean_min \
                                or color_gray_x5_2_1_min_val < color_gray_x3_mean_min or color_gray_x5_2_2_min_val < color_gray_x3_mean_min or color_gray_x5_2_3_min_val < color_gray_x3_mean_min or color_gray_x5_2_4_min_val < color_gray_x3_mean_min or color_gray_x5_2_5_min_val < color_gray_x3_mean_min \
                                or color_gray_x5_3_1_min_val < color_gray_x3_mean_min or color_gray_x5_3_2_min_val < color_gray_x3_mean_min or color_gray_x5_3_3_min_val < color_gray_x3_mean_min or color_gray_x5_3_4_min_val < color_gray_x3_mean_min or color_gray_x5_3_5_min_val < color_gray_x3_mean_min \
                                or color_gray_x5_4_1_min_val < color_gray_x3_mean_min or color_gray_x5_4_2_min_val < color_gray_x3_mean_min or color_gray_x5_4_3_min_val < color_gray_x3_mean_min or color_gray_x5_4_4_min_val < color_gray_x3_mean_min or color_gray_x5_4_5_min_val < color_gray_x3_mean_min \
                                or color_gray_x5_5_1_min_val < color_gray_x3_mean_min or color_gray_x5_5_2_min_val < color_gray_x3_mean_min or color_gray_x5_5_3_min_val < color_gray_x3_mean_min or color_gray_x5_5_4_min_val < color_gray_x3_mean_min or color_gray_x5_5_5_min_val < color_gray_x3_mean_min:
                            yolo_3_3 = False
                            yolo_3_3_index = 0
                            for r in yolo_rects:
                                if r[0] < chip_center[0] and r[1] < chip_center[1] and r[0] + r[2] > chip_center[0] and r[1] + r[3] > chip_center[1]:
                                    yolo_3_3 = True
                                    break
                                yolo_3_3_index += 1
                            yolo_width = 100
                            yolo_height = 100
                            yolo_3_1 = False
                            yolo_3_2 = False
                            yolo_3_4 = False
                            yolo_3_5 = False
                            yolo_1_3 = False
                            yolo_2_3 = False
                            yolo_4_3 = False
                            yolo_5_3 = False
                            yolo_1_1 = False
                            yolo_1_2 = False
                            yolo_2_1 = False
                            yolo_2_2 = False
                            yolo_1_4 = False
                            yolo_1_5 = False
                            yolo_2_4 = False
                            yolo_2_5 = False
                            yolo_4_1 = False
                            yolo_4_2 = False
                            yolo_5_1 = False
                            yolo_5_2 = False
                            yolo_4_4 = False
                            yolo_4_5 = False
                            yolo_5_4 = False
                            yolo_5_5 = False
                            yolo_3_1_index = 0
                            yolo_3_2_index = 0
                            yolo_3_4_index = 0
                            yolo_3_5_index = 0
                            yolo_1_3_index = 0
                            yolo_2_3_index = 0
                            yolo_4_3_index = 0
                            yolo_5_3_index = 0
                            yolo_1_1_index = 0
                            yolo_1_2_index = 0
                            yolo_2_1_index = 0
                            yolo_2_2_index = 0
                            yolo_1_4_index = 0
                            yolo_1_5_index = 0
                            yolo_2_4_index = 0
                            yolo_2_5_index = 0
                            yolo_4_1_index = 0
                            yolo_4_2_index = 0
                            yolo_5_1_index = 0
                            yolo_5_2_index = 0
                            yolo_4_4_index = 0
                            yolo_4_5_index = 0
                            yolo_5_4_index = 0
                            yolo_5_5_index = 0
                            if yolo_3_3:
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1]) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and yolo_width > abs(r[0] - yolo_rects[yolo_3_3_index][0]) \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0]) > yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_width = abs(r[0] - yolo_rects[yolo_3_3_index][0])
                                    if abs(r[0] - yolo_rects[yolo_3_3_index][0]) < yolo_rects[yolo_3_3_index][2] / 2 \
                                            and yolo_height > abs(r[1] - yolo_rects[yolo_3_3_index][1]) \
                                            and abs(r[1] - yolo_rects[yolo_3_3_index][1]) > yolo_rects[yolo_3_3_index][3] / 2:
                                        yolo_height = abs(r[1] - yolo_rects[yolo_3_3_index][1])
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1]) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_3_1 = True
                                        break
                                    yolo_3_1_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1]) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_3_2 = True
                                        break
                                    yolo_3_2_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1]) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_3_4 = True
                                        break
                                    yolo_3_4_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1]) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_3_5 = True
                                        break
                                    yolo_3_5_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0]) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_1_3 = True
                                        break
                                    yolo_1_3_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0]) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_2_3 = True
                                        break
                                    yolo_2_3_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0]) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_4_3 = True
                                        break
                                    yolo_4_3_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0]) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_5_3 = True
                                        break
                                    yolo_5_3_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_1_1 = True
                                        break
                                    yolo_1_1_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_1_2 = True
                                        break
                                    yolo_1_2_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_2_1 = True
                                        break
                                    yolo_2_1_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_2_2 = True
                                        break
                                    yolo_2_2_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_1_4 = True
                                        break
                                    yolo_1_4_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_1_5 = True
                                        break
                                    yolo_1_5_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_2_4 = True
                                        break
                                    yolo_2_4_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] + yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_2_5 = True
                                        break
                                    yolo_2_5_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_4_1 = True
                                        break
                                    yolo_4_1_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_4_2 = True
                                        break
                                    yolo_4_2_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_5_1 = True
                                        break
                                    yolo_5_1_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] + yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_5_2 = True
                                        break
                                    yolo_5_2_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_4_4 = True
                                        break
                                    yolo_4_4_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_4_5 = True
                                        break
                                    yolo_4_5_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_5_4 = True
                                        break
                                    yolo_5_4_index += 1
                                for r in yolo_rects:
                                    if abs(r[1] - yolo_rects[yolo_3_3_index][1] - yolo_height * 2) < yolo_rects[yolo_3_3_index][3] / 2 \
                                            and abs(r[0] - yolo_rects[yolo_3_3_index][0] - yolo_width * 2) < yolo_rects[yolo_3_3_index][2] / 2:
                                        yolo_5_5 = True
                                        break
                                    yolo_5_5_index += 1
                                chip_x5 = chip[int(yolo_rects[yolo_3_3_index][1] - yolo_height*2):int(yolo_rects[yolo_3_3_index][1] + yolo_height * 3), int(yolo_rects[yolo_3_3_index][0] - yolo_width*2):int(yolo_rects[yolo_3_3_index][0] + yolo_width * 3)]
                                chip_x5_h, chip_x5_w, chip_x5_c = chip_x5.shape
                                chip_x5_200_size = (200, 200)
                                if chip_x5_w < chip_x5_h:
                                    chip_x5_200_size = (int(200 * chip_x5_w / chip_x5_h), 200)
                                else:
                                    chip_x5_200_size = (200, int(200 * chip_x5_h / chip_x5_w))
                                chip_x5_200 = cv2.resize(chip_x5, chip_x5_200_size)
                                background[300:300 + chip_x5_200_size[1], 0:chip_x5_200_size[0]] = chip_x5_200
                                background_x5_mean = background[300:500, 200:500]
                                background_x5_mean_h, background_x5_mean_w, background_x5_mean_c = background_x5_mean.shape
                                cv2.line(background_x5_mean, (60, 0), (60, background_x5_mean_h), (0, 0, 0))
                                cv2.line(background_x5_mean, (120, 0), (120, background_x5_mean_h), (0, 0, 0))
                                cv2.line(background_x5_mean, (180, 0), (180, background_x5_mean_h), (0, 0, 0))
                                cv2.line(background_x5_mean, (240, 0), (240, background_x5_mean_h), (0, 0, 0))
                                cv2.circle(background_x5_mean, (40, 20), 20, (0, 255, 0) if yolo_1_1 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (100, 20), 20, (0, 255, 0) if yolo_1_2 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (160, 20), 20, (0, 255, 0) if yolo_1_3 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (220, 20), 20, (0, 255, 0) if yolo_1_4 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (280, 20), 20, (0, 255, 0) if yolo_1_5 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (40, 60), 20, (0, 255, 0) if yolo_2_1 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (100, 60), 20, (0, 255, 0) if yolo_2_2 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (160, 60), 20, (0, 255, 0) if yolo_2_3 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (220, 60), 20, (0, 255, 0) if yolo_2_4 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (280, 60), 20, (0, 255, 0) if yolo_2_5 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (40, 100), 20, (0, 255, 0) if yolo_3_1 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (100, 100), 20, (0, 255, 0) if yolo_3_2 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (160, 100), 20, (0, 255, 0) if yolo_3_3 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (220, 100), 20, (0, 255, 0) if yolo_3_4 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (280, 100), 20, (0, 255, 0) if yolo_3_5 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (40, 140), 20, (0, 255, 0) if yolo_4_1 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (100, 140), 20, (0, 255, 0) if yolo_4_2 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (160, 140), 20, (0, 255, 0) if yolo_4_3 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (220, 140), 20, (0, 255, 0) if yolo_4_4 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (280, 140), 20, (0, 255, 0) if yolo_4_5 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (40, 180), 20, (0, 255, 0) if yolo_5_1 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (100, 180), 20, (0, 255, 0) if yolo_5_2 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (160, 180), 20, (0, 255, 0) if yolo_5_3 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (220, 180), 20, (0, 255, 0) if yolo_5_4 else (0, 0, 255), -1)
                                cv2.circle(background_x5_mean, (280, 180), 20, (0, 255, 0) if yolo_5_5 else (0, 0, 255), -1)
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_1_1_index])) if yolo_1_1 else '0', (0, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_1_2_index])) if yolo_1_2 else '0', (60, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_1_3_index])) if yolo_1_3 else '0', (120, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_1_4_index])) if yolo_1_4 else '0', (180, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_1_5_index])) if yolo_1_5 else '0', (240, 40), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_2_1_index])) if yolo_2_1 else '0', (0, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_2_2_index])) if yolo_2_2 else '0', (60, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_2_3_index])) if yolo_2_3 else '0', (120, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_2_4_index])) if yolo_2_4 else '0', (180, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_2_5_index])) if yolo_2_5 else '0', (240, 80), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_3_1_index])) if yolo_3_1 else '0', (0, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_3_2_index])) if yolo_3_2 else '0', (60, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_3_3_index])) if yolo_3_3 else '0', (120, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_3_4_index])) if yolo_3_4 else '0', (180, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_3_5_index])) if yolo_3_5 else '0', (240, 120), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_4_1_index])) if yolo_4_1 else '0', (0, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_4_2_index])) if yolo_4_2 else '0', (60, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_4_3_index])) if yolo_4_3 else '0', (120, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_4_4_index])) if yolo_4_4 else '0', (180, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_4_5_index])) if yolo_4_5 else '0', (240, 160), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_5_1_index])) if yolo_5_1 else '0', (0, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_5_2_index])) if yolo_5_2 else '0', (60, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_5_3_index])) if yolo_5_3 else '0', (120, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_5_4_index])) if yolo_5_4 else '0', (180, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                background_x5_mean = cv2.putText(background_x5_mean, "{:.2f}".format(float(yolo_scores[yolo_5_5_index])) if yolo_5_5 else '0', (240, 200), cv2.FONT_HERSHEY_DUPLEX, 0.5, (0, 0, 0))
                                if color_pink_x5_1_1:
                                    color_x5_1_1 = yolo_1_1
                                if color_pink_x5_1_2:
                                    color_x5_1_2 = yolo_1_2
                                if color_pink_x5_1_3:
                                    color_x5_1_3 = yolo_1_3
                                if color_pink_x5_1_4:
                                    color_x5_1_4 = yolo_1_4
                                if color_pink_x5_1_5:
                                    color_x5_1_5 = yolo_1_5
                                if color_pink_x5_2_1:
                                    color_x5_2_1 = yolo_2_1
                                if color_pink_x5_2_2:
                                    color_x5_2_2 = yolo_2_2
                                if color_pink_x5_2_3:
                                    color_x5_2_3 = yolo_2_3
                                if color_pink_x5_2_4:
                                    color_x5_2_4 = yolo_2_4
                                if color_pink_x5_2_5:
                                    color_x5_2_5 = yolo_2_5
                                if color_pink_x5_3_1:
                                    color_x5_3_1 = yolo_3_1
                                if color_pink_x5_3_2:
                                    color_x5_3_2 = yolo_3_2
                                if color_pink_x5_3_3:
                                    color_x5_3_3 = yolo_3_3
                                if color_pink_x5_3_4:
                                    color_x5_3_4 = yolo_3_4
                                if color_pink_x5_3_5:
                                    color_x5_3_5 = yolo_3_5
                                if color_pink_x5_4_1:
                                    color_x5_4_1 = yolo_4_1
                                if color_pink_x5_4_2:
                                    color_x5_4_2 = yolo_4_2
                                if color_pink_x5_4_3:
                                    color_x5_4_3 = yolo_4_3
                                if color_pink_x5_4_4:
                                    color_x5_4_4 = yolo_4_4
                                if color_pink_x5_4_5:
                                    color_x5_4_5 = yolo_4_5
                                if color_pink_x5_5_1:
                                    color_x5_5_1 = yolo_5_1
                                if color_pink_x5_5_2:
                                    color_x5_5_2 = yolo_5_2
                                if color_pink_x5_5_3:
                                    color_x5_5_3 = yolo_5_3
                                if color_pink_x5_5_4:
                                    color_x5_5_4 = yolo_5_4
                                if color_pink_x5_5_5:
                                    color_x5_5_5 = yolo_5_5
                                if color_x5_1_1 == yolo_1_1 and color_x5_1_2 == yolo_1_2 and color_x5_1_3 == yolo_1_3 and color_x5_1_4 == yolo_1_4 and color_x5_1_5 == yolo_1_5 \
                                        and color_x5_2_1 == yolo_2_1 and color_x5_2_2 == yolo_2_2 and color_x5_2_3 == yolo_2_3 and color_x5_2_4 == yolo_2_4 and color_x5_2_5 == yolo_2_5 \
                                        and color_x5_3_1 == yolo_3_1 and color_x5_3_2 == yolo_3_2 and color_x5_3_3 == yolo_3_3 and color_x5_3_4 == yolo_3_4 and color_x5_3_5 == yolo_3_5 \
                                        and color_x5_4_1 == yolo_4_1 and color_x5_4_2 == yolo_4_2 and color_x5_4_3 == yolo_4_3 and color_x5_4_4 == yolo_4_4 and color_x5_4_5 == yolo_4_5 \
                                        and color_x5_5_1 == yolo_5_1 and color_x5_5_2 == yolo_5_2 and color_x5_5_3 == yolo_5_3 and color_x5_5_4 == yolo_5_4 and color_x5_5_5 == yolo_5_5:
                                    background = cv2.putText(background, 'OK', (0, 95), cv2.FONT_HERSHEY_DUPLEX, 3, (0, 255, 0), 3)
                                    result.write("OK\n")
                                    result.write("图档符合\n")
                                    cursor.execute(sql_update1 + ' res=\'OK\', res_ch=\'图档符合\', x=\'5X5\' ' \
                                                   + sql_update2 + '\'' + image.replace('\\', '\\\\') + '\'')
                                    conn.commit()
                                else:
                                    background = cv2.putText(background, 'NG', (0, 95), cv2.FONT_HERSHEY_DUPLEX, 3, (0, 0, 255), 3)
                                    result.write("NG\n")
                                    result.write("图档不符\n")
                                    cursor.execute(sql_update1 + ' res=\'NG\', res_ch=\'图档不符\', x=\'5X5\' ' \
                                                   + sql_update2 + '\'' + image.replace('\\', '\\\\') + '\'')
                                    conn.commit()
                            else:
                                chip_x5_200 = chip[chip_center[1] - 100:chip_center[1] + 100, chip_center[0] - 100:chip_center[0] + 100]
                                background[300:500, 0:200] = chip_x5_200
                                background = cv2.putText(background, 'RE', (0, 95), cv2.FONT_HERSHEY_DUPLEX, 3, (255, 0, 0), 3)
                                result.write("RE\n")
                                result.write("复判,找不到中心芯粒\n")
                                cursor.execute(sql_update1 + ' res=\'RE\', res_ch=\'复判,找不到中心芯粒\', x=\'5X5\' ' \
                                               + sql_update2 + '\'' + image.replace('\\', '\\\\') + '\'')
                                conn.commit()
                        else:
                            chip_x5_200 = chip[chip_center[1] - 100:chip_center[1] + 100, chip_center[0] - 100:chip_center[0] + 100]
                            background[300:500, 0:200] = chip_x5_200
                            background = cv2.putText(background, 'OK', (0, 95), cv2.FONT_HERSHEY_DUPLEX, 3, (255, 0, 0), 3)
                            result.write("OK2\n")
                            result.write("忽略,没有定位标志\n")
                            cursor.execute(sql_update1 + ' res=\'OK2\', res_ch=\'忽略,没有定位标志\', x=\'5X5\' ' \
                                           + sql_update2 + '\'' + image.replace('\\', '\\\\') + '\'')
                            conn.commit()
                        cv2.imwrite(res_dir + '\\' + date + '\\' + lot + '\\' + name.replace('.Bmp', '') + '\\result.jpg', background)
                        result.write("5X5\n")
                        result.flush()
                        result.close()
                except Exception as ex:
                    print(ex)
                    exp = open("Exception.txt", mode="a")
                    exp.write(time.strftime("%Y-%m-%d %H:%M:%S ", time.localtime()))
                    exp.write("\n")
                    exp.write(image)
                    exp.write("\n")
                    exp.write(str(ex))
                    exp.write("\n")
                    exp.write("\n")
                    exp.flush()
                    exp.close()
            conn.close()
            # conn = pymysql.connect(host=host, user=user, password=password, database=db)
            # cursor = conn.cursor()
            # for image in source:
            #     cursor.execute(sql_delete1 + '\'' + image.replace('\\', '\\\\') + '\'')
            #     conn.commit()
            # conn.close()
            # time.sleep(1)
            break
