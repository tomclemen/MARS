library(ncdf4)
library(raster)
library(maptools)
library(rgdal)
library(plyr)
library(RColorBrewer)
library(mongolite)
library(jsonlite)

# this script calculates the biomass outtake between two ticks 
# and plot this result

# Connect to mongodb

collection <- "2ba4ed1b-3848-441d-a30e-9669c6d54a6e-KNPGISRasterVegetationLayer"
con <- mongo(collection, url = "mongodb://127.0.0.1:27017/ResultData")

tick <- 0 # will be passed to mongo query

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
lonlat     <- expand.grid(xx,yy)

xyz_aglb1   <- data.frame(cbind(lonlat,as.vector(t(aglb1))))
xyz_aglb1_r <- rasterFromXYZ(xyz_aglb1, res=c(0.008,0.008), crs=NA, digits=5)

biomass_tick0 <- xyz_aglb1_r

# now the same for another tick...

tick <- 8759 # after one year

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
lonlat     <- expand.grid(xx,yy)

xyz_aglb1   <- data.frame(cbind(lonlat,as.vector(t(aglb1))))
xyz_aglb1_r <- rasterFromXYZ(xyz_aglb1, res=c(0.008,0.008), crs=NA, digits=5)

biomass_tick8759 <- xyz_aglb1_r

biomass_diff <- biomass_tick0 - biomass_tick8759

# plot result
data(wrld_simpl)

# format output frame (4 cols, smaller inner and outer margin)
#par( mfrow=c(1,1), mar=c(3,3,1,4), oma=c(0,2,2,1) )

#bm_brk <- seq(0,max(d[,4]), by=100)
#bm_brk <- seq(0,270, by=40)
#bm_brk <- seq(0,90, by=12)
bm_col <- brewer.pal(length(bm_brk),"Greens")

plot(biomass_diff, col=bm_col, main="2094b" )

writeRaster(biomass_diff, filename="~/Downloads/output-raster-data.asc", format="ascii", overwrite=TRUE)

# find position of max value in rasterlayer object

#idx <- which.max(xyz_aglb1_r)
#pos = xyFromCell(xyz_aglb1_r,idx)

