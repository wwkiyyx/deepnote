# opencv 计算机视觉 Computer Vision    

 - OpenCV (Open Source Computer Vision Library: http://opencv.org)           

### Mat 矩阵 Matrix 核心功能 Core functionality

* 读写 显示 imread imwrite namedWindow imshow waitKey 
* np.zeros clone copyTo 
* Point Scalar 
* 绘画 rectangle line ellipse circle fillPoly putText polylines
* 像素操作 img[100,100] img[100,100,0] item itemset
* 属性 shape size dtype   
* ROI img[280:340, 330:390]
* 合并 merge 拆分 split img[:,:,0]
* 边框 copyMakeBorder
* 摄像头 VideoCapture isOpened read release 
* 鼠标事件 setMouseCallback
* 滑动条 createTrackbar getTrackbarPos
* 转图 cvtColor
* resize warpAffine getRotationMatrix2D getAffineTransform getPerspectiveTransform warpPerspective
* normalize归一
* mean均值

### imgproc 图像处理 Image Processing

* filter2D Smoothing平滑处理 Blur模糊 GaussianBlur高斯模糊 MedianBlur中值模糊 BilateralFilter双边滤波 
* Erode腐蚀 Dildate膨胀 Morphology形态学 Open开 Close闭 Morphological Gradient 形态学梯度 TopHat BlackHat getStructuringElement
* Pyramids金字塔 PyrUp上采样 PyrDown下采样 
* threshold 阈值 二值化 adaptiveThreshold inrange
* filter2D 线性滤波 卷积 
* copyMakeBorder加边框 
* 边缘 Sobel梯度求导 Laplacian拉普拉斯 Canny边缘检测 
* HoughLines霍夫直线检测 HoughCircles霍夫圆检测 
* remap 
* warpAffine 
* histogram equalizeHist calcHist compareHist calcBackProject 
* matchTemplate匹配模板 minMaxLoc
* findContours寻找轮廓 drawContours画轮廓 convexHull凸包 boundingRect外接矩形 minEnclosingCircle外接圆 minAreaRect fitEllipse moments重心 contourArea面积 arcLength周长 fitLine matchShapes pointPolygonTest convexityDefects
* 计时 getTickCount getTickFrequency 
* 算数操作 add addWeighted bitwise_not bitwise_and
* 傅里叶 np.fft
* watershed 填充
* grabCut 去背景

### calib3d 标定 Camera Calibration 相机校准

* findChessboardCorners findCirclesGrid cornerSubPix drawChessboardCorners calibrateCamera getOptimalNewCameraMatrix undistort initUndistortRectifyMap remap projectPoints solvePnP
* StereoBM_create 

### features2d 特征 2D Features

* cornerHarris cornerSubPix
* goodFeaturesToTrack
* SIFT_create detect drawKeypoints detectAndCompute
* xfeatures2d SURF_create detectAndCompute getHessianThreshold setHessianThreshold getUpright setUpright 
* FastFeatureDetector_create
* StarDetector_create BriefDescriptorExtractor_create 
* ORB_create
* detectAndCompute BFMatcher match
* FlannBasedMatcher knnMatch findHomography perspectiveTransform 
* SurfFeatureDetector KeyPoint drawKeypoints SurfDescriptorExtractor BFMatcher drawMatches 
* FlannBasedMatcher findHomography perspectiveTransform

### Cascade Classifier 级联分类器

* CascadeClassifier 

### 机器学习 machine learning

* knn k-Nearest Neighbour 
* SVM 
* K-Means

### 摄影

* 去噪 fastNlMeansDenoising fastNlMeansDenoisingColored
* 修补 inpaint 
* HDR 平衡