library(ncdf4)
library(raster)
library(maptools)
library(rgdal)
library(plyr)
library(RColorBrewer)

library(mongolite)
library(jsonlite)

library(ggplot2)
library(ggspatial)
library(gridExtra)
library(ggpubr)

# R colors : http://sape.inf.usi.ch/quick-reference/ggplot2/colour 
# ggplot2 options : https://gist.github.com/jalapic/9a1c069aa8cee4089c1e
#                   https://bhaskarvk.github.io/user2017.geodataviz/notebooks/02-Static-Maps.nb.html
# Point label/annotations: http://eriqande.github.io/rep-res-web/lectures/making-maps-with-R.html
# marginal plots: http://geog.uoregon.edu/bartlein/courses/geog490/week04-raster.html
#                 https://cran.r-project.org/web/packages/ggExtra/README.html

# add plot to plot layout
# ggplot themes
# http://felixfan.github.io/ggplot2-remove-grid-background-margin/
# http://www.sthda.com/english/wiki/ggplot2-themes-and-background-colors-the-3-elements

# https://github.com/tidyverse/ggplot2/issues/2579
# Allow several simultaneous colour scales NOT IMPLEMENTED: https://github.com/tidyverse/ggplot2/issues/578

# Density calc for heatmap/point. 
# Used: http://slowkow.com/notes/ggplot2-color-by-density/
# Another options: 
#   http://khalo.github.io/r/spatial/2015/04/24/heatmap/
#   https://www.r-graph-gallery.com/2d-density-plot-with-ggplot2/
#   https://bhaskarvk.github.io/user2017.geodataviz/notebooks/02-Static-Maps.nb.html
#   https://www4.stat.ncsu.edu/~reich/SpatialStats/code/GGplot.html

#e1abaaf8-ec2a-4aef-8761-b1f4fcbb57c2

# Connect to mongodb
ticks <- c(8615) # will be passed to mongo query

#collectionRasterLayer <- "d75ea01f-2194-4a54-a9c8-a23c54bc73ec-KNPGISRasterVegetationLayer"
#conLayer <- mongo(collectionRasterLayer, url = "mongodb://readUser:mongoRead0@jcd.info.tm:27017/ResultData")

collectionRasterLayer <- "e1abaaf8-ec2a-4aef-8761-b1f4fcbb57c2-KNPGISRasterVegetationLayer"
conLayer <- mongo(collectionRasterLayer, url = "mongodb://127.0.0.1:27017/ResultData")

collectionAgents <- "e1abaaf8-ec2a-4aef-8761-b1f4fcbb57c2-kf"
#conAgents <- mongo(collectionAgents, url = "mongodb://readUser:mongoRead0@jcd.info.tm:27017/ResultData")
conAgents <- mongo(collectionAgents, url = "mongodb://127.0.0.1:27017/ResultData")

# output/input
outputRasterFilesAsGTiff <- TRUE
outputPngFilename <- "./18-%TICK%.png" # TICK placeholder will be replaced via regex
png( file=gsub("*%TICK%*",1,outputPngFilename), height=3754, width=3240, res=200 )
#png( file=gsub("*%TICK%*",1,outputPngFilename), height=842, width=595, res=72 ) #825

#https://www.rdocumentation.org/packages/raster/versions/2.6-7/topics/shapefile
rivers <- shapefile("./gis_vector_rivers/gis_rivers.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)
borders <- shapefile("./gis_raster_border/gis_raster_border.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)
waterpoints <- shapefile("./waterpoints.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)


# settings
dividerPerCell <- 100 # 10000
bm_brk <- seq(0,400, by=10)

# Converting lat/long to DM, DMS see link
# https://stackoverflow.com/questions/45698588/revisiting-the-format-latitude-and-longitude-axis-labels-in-ggplot
scale_x_longitude <- function(xmin=-180, xmax=180, step=1, ...) {
  xbreaks <- seq(xmin,xmax,step)
  xlabels <- unlist(lapply(xbreaks, function(x) ifelse(x < 0, parse(text=paste0(x,"^o", "*W")), ifelse(x > 0, parse(text=paste0(x,"^o", "*E")),x))))
  return(scale_x_continuous("Longitude", breaks = xbreaks, labels = xlabels, expand = c(0, 0)))
}
scale_y_latitude <- function(ymin=-90, ymax=90, step=0.5, ...) {
  ybreaks <- seq(ymin,ymax,step)
  ylabels <- unlist(lapply(ybreaks, function(x) ifelse(x < 0, parse(text=paste0(x,"^o", "*S")), ifelse(x > 0, parse(text=paste0(x,"^o", "*N")),x))))
  return(scale_y_continuous("Latitude", breaks = ybreaks, labels = ylabels, expand = c(0, 0)))
} 

# density calc
get_density <- function(x, y, n = 100) {
  dens <- MASS::kde2d(x = x, y = y, n = n)
  ix <- findInterval(x, dens$x)
  iy <- findInterval(y, dens$y)
  ii <- cbind(ix, iy)
  return(dens$z[ii])
}

plots <- list()
i<-1
for (tick in ticks) {
  # get data
  iterRasterLayer <- conLayer$iterate(sprintf('{"tick":%i}', tick))
  rasterLayerJson <- iterRasterLayer$json()
  rL <- fromJSON(rasterLayerJson)
  
  rl_CellSizeInDeg <- rL$cellSizeInDegree
  # rasterFromXYZ expects centroids, so we're shifting both LL/UR 
  rL_LLBound <- rL$lowerLeftBound + (rl_CellSizeInDeg/2)
  rL_URBound <- rL$upperRightBound + (rl_CellSizeInDeg/2)
  
  # transform data because of UL -> LL mapping
  for(col in 1:ncol(rL$gridData)) {
    rL$gridData[,col] <- rev(rL$gridData[,col])
  }
  
  # apply division to match dvgm data
  rL$gridData <- rL$gridData/dividerPerCell
  
  # iterate by x,y coords and fill coords matrices
  xx <- seq(rL_LLBound[1], rL_URBound[1]-rl_CellSizeInDeg, by=rl_CellSizeInDeg)
  yy <- seq(rL_LLBound[2],rL_URBound[2]-rl_CellSizeInDeg, by=rl_CellSizeInDeg)
  mx  <- matrix( NA, length(yy), length(xx) )
  my  <- matrix( NA, length(yy), length(xx) )
  for ( x in 1:ncol(rL$gridData) )
  {
    for ( y in 1:nrow(rL$gridData) )
    {
      mx[y,x]<- xx[x]
      my[y,x]<- yy[y]
    }
  }
  
  aglb1 <- rL$gridData
  lonlat     <- expand.grid(xx,yy)

  xyz_aglb1   <- data.frame(cbind(lonlat,as.vector(t(aglb1))))
  xyz_aglb1_r <- rasterFromXYZ(xyz_aglb1, crs=4326, digits=5)
  
  #write data to geotiff
  #https://www.rdocumentation.org/packages/raster/versions/2.6-7/topics/writeRaster
  if(outputRasterFilesAsGTiff) {
    rf <- writeRaster(xyz_aglb1_r, filename=sprintf("./output-raster-data_tick--%i.tif",tick), format="GTiff", overwrite=TRUE)
  }
  ### agent data
  agentData <- conAgents$find(sprintf('{"Tick":%i, "Properties.AgentType":"%s"}', tick, "Elephant"), limit=100000)
  agentDataCount <- nrow(agentData)
  print(agentDataCount)
  
  xVals = c()
  yVals = c()
  for(pos in agentData$Position){
    xVals <- append(xVals,pos[1])
    yVals <- append(yVals,pos[2])
  }
  agentDataPos <- data.frame(x=xVals,y=yVals)
  if(outputRasterFilesAsGTiff) {
    write.csv(agentDataPos, file = "agentDataPos.csv")
  }
  
  # Alpha density only works on one plot ( Error: Aesthetics must be either length 1 or the same as the data )
  if(length(ticks) == 1) {
    density <- get_density(agentDataPos$x, agentDataPos$y, 100)
    pointsPlot <- geom_point(data=agentDataPos,aes(x, y, alpha=density),size=0.5,color="orange")
  } else {
    pointsPlot <- geom_point(data=agentDataPos,aes(x, y),alpha=0.5,size=0.5,color="orange")
  }
  
  plots[[i]] <- ggplot() +
    theme(panel.background = element_blank(),axis.text = element_text(size=12)) +
    coord_sf(crs = 4326) +
    layer_spatial(xyz_aglb1_r, interpolate = FALSE) +
    scale_fill_gradient(low="darkseagreen1",high="springgreen4", name="Biomass UNIT", breaks = bm_brk)+ 
    scale_x_longitude(30.5,32.5,1)+
    scale_y_latitude(-25.5,-21.5,1)+
    labs(title = sprintf("Tick: %i \n Agents: %i",tick,agentDataCount)) + #x = "", y = "",
    #geom_hex(data=agentDataPos,aes(x, y, fill=..count..), bins=50)+ 
    pointsPlot+
    geom_polygon(data=rivers, aes(x=long, y=lat, group=group), 
                   fill=NA,color="steelblue4", size=0.4)+
    geom_point(data=waterpoints, aes(x=long, y=lat), 
                 fill=NA,color="steelblue4", size=0.4)+
    geom_polygon(data=borders, aes(x=long, y=lat, group=group), 
                   fill=NA,color="azure4", size=0.5)
   # )
  i<-i+1
}

# arrange: 
#         http://rstudio-pubs-static.s3.amazonaws.com/2852_379274d7c5734f979e106dcf019ec46c.html
#         http://www.sthda.com/english/wiki/print.php?id=177
rows <- ceiling(length(ticks)/3)
ggarrange( plotlist = plots, nrow = rows, ncol = min(length(ticks),3), common.legend = TRUE, legend="top") # nrow = 1, ncol = 2,

