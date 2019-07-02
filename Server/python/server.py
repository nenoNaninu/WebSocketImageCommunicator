# -*- coding: utf-8 -*-

import cv2
import numpy as np
from flask import Flask,request,helpers,make_response
from PIL import Image
import io
from datetime import datetime

app = Flask("imageCovertServer")

@app.route("/detect/", methods=["POST"])
def detect(): 
    if request.headers["Content-Type"] == "application/octet-stream":
        # 1280 * 720
        width = int(request.headers['width'])
        height = int(request.headers['height'])
        print("width",width)
        print("height", height)
        print("detecting")
        data = request.data
        img = np.frombuffer(data,dtype = 'uint8').reshape([height,width,-1])
        file_name = datetime.now().strftime("%Y%m%d%H%M%S")+ ".png"
        if img.shape[2] != 1 and img.shape[2] != 3 and img.shape[2] != 4:
            img = np.frombuffer(data,dtype = 'uint16').reshape([height,width,-1])
            np.save(file_name,img)
            img = img/img.max()*255
            file_name = datetime.now().strftime("%Y%m%d%H%M%S")+ "chan1.png"
            
        print(img.shape)

        # dstimg = cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA)
        check = cv2.imwrite(file_name,img)
        print(file_name)
        print(check)
        response = make_response()
        response.data = r"""[  { "RecipeName": "left_top", "Calorie": 1820.0,"X": 0, "Y": 0  },  { "RecipeName": "right_bottom", "Calorie": 0.0,"X": 1280, "Y": 720  }]"""
        response.headers["Content-type"] = "application/json"
        return response

if __name__ == "__main__":
    app.run(host="0.0.0.0",port=5000)