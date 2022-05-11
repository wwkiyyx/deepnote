import setuptools

with open("README.md", "r", encoding="utf-8") as fh:
    long_description = fh.read()

setuptools.setup(
    name="opencvpro",
    version="0.0.1",
    author="wangwenkai",
    author_email="670405621@qq.com",
    description="A opencv package",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/wwkiyyx/wangwenkai40/OpenCV/OpenCVPro",
    project_urls={
        "Bug Tracker": "https://github.com/wwkiyyx/wangwenkai40/OpenCV/OpenCVPro",
    },
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
    package_dir={"": "src"},
    packages=setuptools.find_packages(where="src"),
    python_requires=">=3.6",
)