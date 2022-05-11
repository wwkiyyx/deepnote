import cv2
from matplotlib import pyplot as plt

def showGray(mat) :
    mat_color = cv2.cvtColor(mat, cv2.COLOR_GRAY2RGB)
    mat_show = mat_color[:,:,::-1]
    plt.figure(figsize=(10,10))
    plt.imshow(mat_show)

def showColor(mat) :
    mat_show = mat[:,:,::-1]
    plt.figure(figsize=(10,10))
    plt.imshow(mat_show)
    