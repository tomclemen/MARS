library(ncdf4)
library(raster)
library(maptools)
library(rgdal)
library(plyr)
library(RColorBrewer)

library(mongolite)
library(jsonlite)

library(Rmisc)
library(ggplot2)
library(ggspatial)
library(gridExtra)

# Connect to mongodb
ticks <- c(100,300) # will be passed to mongo query
collection <- "cb2eabc9-daf2-4c98-8655-b88585831487-KNPGISRasterVegetationLayer"
con <- mongo(collection, url = "mongodb://127.0.0.1:27017/ResultData")

# output/input
outputPngFilename <- "./griddata-mongo-%TICK%.png" # TICK placeholder will be replaced via regex
#l <- readOGR(dsn="/Users/thc/ownCloud/03 Land/Clemen et al./R/boundary", layer="boundary")
#https://www.rdocumentation.org/packages/raster/versions/2.6-7/topics/shapefile
rivers <- shapefile("./gis_vector_rivers/gis_rivers.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)
borders <- shapefile("./gis_raster_border/gis_raster_border.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)

# settings
yearTitlePng <- 1999
dividerPerCell <- 10000

# plot layout

# format output frame (4 cols, smaller inner and outer margin)
#plot_mfrow <- c(ceiling(sqrt(length(ticks))),floor(sqrt(length(ticks))))
#par( mfrow=c(2,2))#, mar=c(3,3,1,4), oma=c(0,2,2,1) )

#bm_brk <- seq(0,max(d[,4]), by=100)
#bm_brk <- seq(0,270, by=40)
bm_brk <- seq(0,90, by=12)
bm_col <- brewer.pal(length(bm_brk),"Greens")

png( file=gsub("*%TICK%*",1,outputPngFilename), height=825, width=595 )
# do it!
plots <- list()
i<-1
for (tick in ticks) {
  #png( file=gsub("*%TICK%*",tick,outputPngFilename), height=825, width=595 )
  # get data
  iterRasterLayer <- con$iterate(sprintf('{"tick":%i}', tick))
  rasterLayerJson <- iterRasterLayer$json()
  rL <- fromJSON(rasterLayerJson)
  
  rL_LLBound <- rL$lowerLeftBound
  rL_URBound <- rL$upperRightBound
  rl_CellSizeInDeg <- rL$cellSizeInDegree
  
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
  
  # from original code
  aglb1 <- rL$gridData
  lonlat     <- expand.grid(xx-0.25,yy-0.25)
  
  xyz_aglb1   <- data.frame(cbind(lonlat,as.vector(t(aglb1))))
  xyz_aglb1_r <- rasterFromXYZ(xyz_aglb1, res=c(0.5,0.5), crs=NA, digits=5)
  #print(xyz_aglb1_r)
  
  #write data to geotiff
  #https://www.rdocumentation.org/packages/raster/versions/2.6-7/topics/writeRaster
  rf <- writeRaster(xyz_aglb1_r, filename=sprintf("./output-raster-data_tick-%i.tif",tick), format="GTiff", overwrite=TRUE)
  
  # add plot to plot layout
  plots[[i]] <- plot(xyz_aglb1_r, col=bm_col, breaks=bm_brk, main=tick)
  
  #plot(l, add=T)
  #plot(rivers, add=TRUE, col="blue")
  #legend ('topright', legend = 'River', lty = 1, lwd = 2, col = 'blue', bty = "n")
  #plot(borders, add=TRUE,col=rgb(red=0,blue=0,green=0,alpha=0.0),border="black")
  
  data(wrld_simpl)
  #plot(wrld_simpl,add=T)
  i<-i+1
}

multiplot(plotlist = plots, cols = 3)

