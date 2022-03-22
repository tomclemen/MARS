library(ncdf4)
library(raster)
library(maptools)
library(rgdal)
library(plyr)
library(RColorBrewer)

library(mongolite)
library(jsonlite)

# Connect to mongodb
tick <- 100 # will be passed to mongo query
collection <- "f6ae7ad2-cfbf-4917-af79-263e102e2052-KNPGISRasterVegetationLayer"
con <- mongo(collection, url = "mongodb://127.0.0.1:27017/ResultData")

# output/input
png( file="~/Downloads/griddata-mongo-test.png", height=825, width=595 )
#l <- readOGR(dsn="~/ownCloud/03 Land/Clemen et al./R/boundary", layer="boundary")
#https://www.rdocumentation.org/packages/raster/versions/2.6-7/topics/shapefile
rivers <- shapefile("~/ownCloud/MARS_Modelle/MARS_KNP/Water/gis_vector_rivers/gis_rivers.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)
borders <- shapefile("~/ownCloud/MARS_Modelle/MARS_KNP/Fence/gis_vector_knp_border/gis_knp_border_checked.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)

# settings
yearTitlePng <- 2018
dividerPerCell <- 10000

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

#write data to geotiff
#https://www.rdocumentation.org/packages/raster/versions/2.6-7/topics/writeRaster
rf <- writeRaster(xyz_aglb1_r, filename="~/Downloads/output-raster-data.tif", format="GTiff", overwrite=TRUE)
# ---------------------------------------------------------------------
# --- Plots -----------------------------------------------------------
# ---------------------------------------------------------------------
data(wrld_simpl)

# format output frame (4 cols, smaller inner and outer margin)
par( mfrow=c(1,1), mar=c(3,3,1,4), oma=c(0,2,2,1) )

#bm_brk <- seq(0,max(d[,4]), by=100)
#bm_brk <- seq(0,270, by=40)
bm_brk <- seq(0,90, by=12)
bm_col <- brewer.pal(length(bm_brk),"Greens")

plot(xyz_aglb1_r, col=bm_col, breaks=bm_brk, main=yearTitlePng )
#plot(l, add=T)
plot(rivers, add=T, col="blue")
#legend ('topright', legend = 'River', lty = 1, lwd = 2, col = 'blue', bty = "n")
plot(borders, add=T,col=rgb(red=0,blue=0,green=0,alpha=0.0),border="black")
#plot(wrld_simpl,add=T)
graphics.off()

