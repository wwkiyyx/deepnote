FROM wwkiyyx/baota:ubuntu1

EXPOSE 80
EXPOSE 8888

RUN apt-get update
RUN apt-get upgrade -y
RUN apt-get install libglib2.0-dev -y
RUN apt-get install libgl1-mesa-glx -y
RUN apt-get install python3-pip -y 
RUN git clone https://github.com/ultralytics/yolov3
RUN git clone https://github.com/ultralytics/yolov5
RUN pip install -r ./yolov3/requirements.txt
RUN cd yolov3 && python3 detect.py --source data/images --weights yolov3.pt  
RUN cd yolov3 && python3 detect.py --source data/images --weights yolov3-spp.pt
RUN cd yolov3 && python3 detect.py --source data/images --weights yolov3-tiny.pt
RUN cd yolov5 && python3 detect.py --source data/images --weights yolov5s.pt
RUN cd yolov5 && python3 detect.py --source data/images --weights yolov5m.pt
RUN cd yolov5 && python3 detect.py --source data/images --weights yolov5l.pt
RUN cd yolov5 && python3 detect.py --source data/images --weights yolov5n.pt
RUN cd yolov5 && python3 detect.py --source data/images --weights yolov5x.pt
RUN pip install tensorflow
RUN pip install tensorflow-hub

