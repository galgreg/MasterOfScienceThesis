# Master of Science Thesis
The project has been created in order to receive the **Master of Science** degree. It contains of two main parts:
1. The thesis - document written in Polish language. The theme is:
  ,\,***Application of convolutional neural network in the process of car steering inside simulated environment***''
3. The application - an appendix to the thesis. It represents the practical aspects of thesis.

The subject of thesis was machine learning techniques used to solve the problem of autonomous car control. 
As a part of thesis, the simple system has been created, which runs the convolutional neural network training based on visual observations provided by Learning Environment. The task of neural network was driving a car through one of prepared racetracks. In order to train the networks successfully, I took advantage of [PPO](https://openai.com/blog/openai-baselines-ppo/) algorithm.

## Implementation details
Thesis has been created in [LaTeX](https://www.latex-project.org/), and later compiled to PDF file. The source code of thesis is stored in *latex_doc* directory.
To create the application, a lot of technologies have been used. Below is the list of most important of them, together with their version numbers:

1. [Unity](https://docs.unity3d.com/2020.3/Documentation/Manual/index.html) - 2020.3.28f
2. C# - Mono 6.12.0.122
3. Python - 3.9.6
4. [Unity ML-Agents](https://github.com/Unity-Technologies/ml-agents/tree/release_19) - Release 19

## How to build & run the application

### 1. Install Python 3
Version 3.9.6 is preferred. Detailed description of installation process is available [here](https://realpython.com/installing-python/).

### 2. Install Unity
To install Unity, follow the [official documentation](https://docs.unity3d.com/2019.1/Documentation/Manual/GettingStartedInstallingUnity.html).

### 3. Install Conda
Please follow [this guide](https://docs.conda.io/projects/conda/en/latest/user-guide/install/index.html) to install Conda. 

### 4. Clone Git repo
```
git clone https://github.com/galgreg/MasterScienceThesis.git
```
### 5. Create virtual environment and install dependencies
Go to `application` directory and call below commands:
```
conda create --name <your-env-name> --file requirements.txt
conda activate <your-env-name>
```
### 6. Run training
1. Launch Unity editor and ensure, that learning environment is properly configured to run training.
2. In terminal, go to `application/unity_mla_environment` directory and call command:
```
mlagents-learn MasterRacingEnvs/Assets/MyTrainingConfig/<config_file> --run-id=<run_id> --results-dir=MasterRacingEnvs/Assets/MyTrainingResults
```
where `<config_file>` is one of prepared config file and `<run_id>` is unique identifier for this training.
Example of usage:
```
mlagents-learn MasterRacingEnvs/Assets/MyTrainingConfig/RaceTrack_1_PPO.yaml --run-id=RaceTrack_1_PPO
```
3. Click play button inside Unity editor to start training.
### 7. Check help to see how to run training
```
mlagents-learn --help
```

## Acknowledgements
Special thanks for dr [Rafał Skinderowicz](https://www.researchgate.net/profile/Rafat_Skinderowicz), who was my promoter and helped me a lot while writing the thesis.

## Terms of use
Author takes no responsibility for any damage or loss caused by improper use of above project.
