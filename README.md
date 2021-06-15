# ChiaTransit
A multi platform command line application for sequentially transferring complete Chia plot files from one location to another. This is designed for plotters who copy their finished plots to a local SSD or HDD before using a script to move those plots to a final destination.

This replaces the need for a Powershell script or batch files and provides real time progress information of the file copy operation and the outstanding plot queue:

![image](https://user-images.githubusercontent.com/22151993/120900452-c4f23380-c62c-11eb-9f2e-e142a6192df6.png)

The application will follow the same pattern as the plotter and ensure that the files are copied to the final destination suffixed with ```.tmp``` until they're complete to prevent the farmer from showing them as invalid plots.

## Buy Me A Coffee! :coffee:

I made this for my own needs and don't expect anything in return. However, if you find it useful and would like to buy me a coffee, feel free to do it at [__Buy me a coffee! :coffee:__](https://buymeacoff.ee/djdd87). This is entirely optional, but would be appreciated! Or even better, help supported this project by contributing changes.

Chia: xch1qk9t6rrjv3z6j3u69tet5lw3cf9zrr56vhrl7xz9ezj0q80fcjfs9ytktr

# Installation

Download the respective zip file from [releases](https://github.com/djdd87/ChiaTransit) for your OS and architecture.

## Linux
Extract all contents to a folder and execute using ./chia-transit --s {source(s)} --d {destination(s)}.

## Windows
Extract all contents to a folder and execute using .\chia-transit --s {source(s)} --d {destination(s)}.

# Examples 

## Linux
* ```./chia-transit --s C:\Chia\Temp --d D:\Chia\Final```
* ```./chia-transit --s C:\Chia\Temp --d "D:\Final Dir"```
* ```./chia-transit --s C:\Chia\Temp --d \\nas\final```
* ```./chia-transit --s C:\Chia\Temp D:\Chia\Temp --d \\nas\final```
* ```./chia-transit --s C:\Chia\Temp D:\Chia\Temp --d \\nas\final\farm1 \\nas\final\farm2```

## Windows
* ```.\chia-transit --s C:\Chia\Temp --d D:\Chia\Final```
* ```.\chia-transit --s C:\Chia\Temp --d "D:\Final Dir"```
* ```.\chia-transit --s C:\Chia\Temp --d \\nas\final```
* ```.\chia-transit --s C:\Chia\Temp D:\Chia\Temp --d \\nas\final```
* ```.\chia-transit --s C:\Chia\Temp D:\Chia\Temp --d \\nas\final\farm1 \\nas\final\farm2```


# CLI Reference

## Source (```--s```, ```--source```)
Specifies the source directory from which to check for new plot files. Separate each source directly with a blank space. Directory paths with spaces must be enclosed in double quotes.

## Destination (```--d```, ```--destination```)
Specifies the destination directory where the finished plot files should be moved. Separate each destination directly with a blank space. Directory paths with spaces must be enclosed in double quotes. 

ChiaTransit will move through the destination drives in sequence, i.e. with 3 plots to copy and 3 destination directories A, B and C, each drive will receive 1 plot. With 2 destination directories A will receive 2 plots and B will receive 1; when the 4th plot is moved, it will be sent to B.

# TODO
* Add a check for whether the destination drive is full
* Allow the max number of copy operations to be set.
