# ChiaTransit
A multi platform command line application for sequentially transferring complete Chia plot files from one location to another. This is designed for plotters who copy their finished plots to a local SSD or HDD before using a script to move those plots to a final destination.

This replaces the need for a Powershell script or batch files and provides real time progress information of the file copy operation and the outstanding plot queue:

![image](https://user-images.githubusercontent.com/22151993/120900452-c4f23380-c62c-11eb-9f2e-e142a6192df6.png)

The application will follow the same pattern as the plotter and ensure that the files are copied to the final destination suffixed with ```.tmp``` until they're complete to prevent the farmer from showing them as invalid plots.

# CLI Reference

## Examples
* ```ChiaTransit --s C:\Chia\Temp --d D:\Chia\Final```
* ```ChiaTransit --s C:\Chia\Temp --d "D:\Final Dir"```
* ```ChiaTransit --s C:\Chia\Temp --d \\\\nas\final```

## Source (```--s```, ```--source```)
Specifies the source directory from which to check for new plot files.

## Destination (```--d```, ```--destination```)
Specifies the destination directory where the finished plot files should be moved.

# TODO
* Add multiple source directories
* Add multiple destination directories
* Add a check for whether the destination drive is full
* Allow the max number of copy operations to be set.
